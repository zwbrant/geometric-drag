using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;

[RequireComponent(typeof(MeshFilter))]
public class FuckinAbout : MonoBehaviour
{
    [Range(0, 1f)]
    public float DragMulti = .01f;
    public bool EnableBurst = true;
    public int BatchSize = 32;
    public bool DebugAngleMags = true;
    public bool DebugForceVectors = false;
    public bool DebugVelocityVector = false;
    public bool UseSimpleDrag = false;


    protected Vec3[] normals;
    private Mesh _mesh;
    private Rigidbody _rBody;
    private Color32[] _colors;


    // Start is called before the first frame update
    void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        _rBody = GetComponent<Rigidbody>();
        if (_rBody == null)
            _rBody = gameObject.AddComponent<Rigidbody>();

        normals = new Vec3[_mesh.triangles.Length / 3];
        _colors = new Color32[_mesh.vertices.Length];

        _nVertices = new NativeArray<Vec3>(_mesh.vertices, Allocator.Persistent);
        _nTriangles = new NativeArray<int>(_mesh.triangles, Allocator.Persistent);
    }


    private NativeArray<int> _nTriangles;
    private NativeArray<Vec3> _nVertices;

    private NativeArray<Vec3> _nDragForces;
    private NativeArray<Vec3> _nMidpoints;
    private NativeArray<float> _nAngleMags;


    private JobHandle _jHandle;
    private void Update()
    {
        if (!EnableBurst)
            return;

        _nDragForces = new NativeArray<Vec3>(_mesh.triangles.Length / 3, Allocator.TempJob);
        _nMidpoints = new NativeArray<Vec3>(_mesh.triangles.Length / 3, Allocator.TempJob);
        _nAngleMags = new NativeArray<float>(_mesh.triangles.Length / 3, Allocator.TempJob);


        var job = new VerletJob() {
            vertices = _nVertices,
            triangles = _nTriangles,
            dragForces = _nDragForces,
            midpoints = _nMidpoints,
            rotation = transform.rotation,
            position = transform.position,
            localScale = transform.localScale,
            velocity = _rBody.velocity,
            angleMags = _nAngleMags,
            dragMulti = DragMulti,
            useSimpleDrag = UseSimpleDrag
        };


         _jHandle = job.Schedule(_mesh.triangles.Length / 3, BatchSize);
    }

    private void LateUpdate()
    {
        if (!EnableBurst)
            return;

        _jHandle.Complete();

        for (int i = 0; i < _nDragForces.Length; i++)
        {

            if (DebugAngleMags)
                UpdateDebugColors(i, _nAngleMags[i]);

            if (float.IsNaN(_nDragForces[i].x))
                continue;
            _rBody.AddForceAtPosition(_nDragForces[i], _nMidpoints[i]);

            if (DebugForceVectors)
                Debug.DrawLine(_nMidpoints[i], _nMidpoints[i] + _nDragForces[i], Color.yellow);

        }

        if (DebugAngleMags)
            _mesh.colors32 = _colors;

        _nDragForces.Dispose();
        _nMidpoints.Dispose();
        _nAngleMags.Dispose();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (EnableBurst)
            return;

        Vec3 pos = transform.position;
        Vec3 dragVect = -_rBody.velocity;

        if (dragVect.magnitude <= 0)
            return;

        if (DebugVelocityVector)
            Debug.DrawLine(pos, pos + dragVect);

        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            Vec3 v1, v2, v3;
            v1 = transform.TransformPoint(_nVertices[_nTriangles[i]]);
            v2 = transform.TransformPoint(_nVertices[_nTriangles[i + 1]]);
            v3 = transform.TransformPoint(_nVertices[_nTriangles[i + 2]]);

            var tri = new Triangle(v1, v2, v3);

            // save triangle normal
            normals[i / 3] = tri.Normal;

              // calculate the angle of this triangles resistance
            var cosAngle = Vec3.Dot(tri.Normal, dragVect) / (dragVect.magnitude * tri.Normal.magnitude);
            var angle = Mathf.Acos(cosAngle);

            // magnitude of drag: 180 = 1, 135 = 0.5, < 90 = 0
            var surfAngleMag = Mathf.Clamp((angle - Mathf.PI / 2) / (Mathf.PI / 2), 0, 1);

            //Debug.DrawLine(midPoint, midPoint + (surfNorm * triArea), Color.red);

            var fluidDensity = 1f;
            var velSqu = _rBody.velocity.sqrMagnitude;

            var dragForce2 = -.5f * fluidDensity * velSqu * tri.Area * surfAngleMag * Vec3.Normalize(_rBody.velocity); 

            //var dragForce = -tri.Normal * tri.Area * surfAngleMag * dragVect.magnitude * DragMulti;
                
            if (dragForce2.magnitude > 0f)
                _rBody.AddForceAtPosition(dragForce2, tri.Midpoint);

            //Debug.DrawLine(tri.Midpoint, tri.Midpoint + Vector3.Reflect(dragVect, tri.Normal) * .1f, Color.green);

            if (DebugForceVectors)
                Debug.DrawLine(tri.Midpoint, tri.Midpoint + dragForce2, Color.yellow);

            UpdateDebugColors(i / 3, surfAngleMag);
        }

        _mesh.colors32 = _colors;
    }


    Color32 _red = new Color32(255, 0, 0, 1);
    Color32 _green = new Color32(0, 255, 0, 1);

    private void UpdateDebugColors(int triIndex, float dragMag)
    {
        var index = triIndex * 3;
        var c = Color32.Lerp(_green, _red, dragMag);
        _colors[_nTriangles[index]] = c;
        _colors[_nTriangles[index + 1]] = c;
        _colors[_nTriangles[index + 2]] = c;
    }

    public struct VerletJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> triangles;
        [ReadOnly]
        public NativeArray<Vec3> vertices;
        public Quaternion rotation;
        public Vec3 position;
        public Vec3 localScale;
        public Vec3 velocity;
        public float dragMulti;
        public bool useSimpleDrag;

        public NativeArray<Vec3> dragForces;
        public NativeArray<Vec3> midpoints;
        public NativeArray<float> angleMags;

        public void Execute(int index)
        {
            int triIndex = index * 3;

            var v1 = rotation * Vec3.Scale(vertices[triangles[triIndex]], localScale) + position;
            var v2 = rotation * Vec3.Scale(vertices[triangles[triIndex + 1]], localScale) + position;
            var v3 = rotation * Vec3.Scale(vertices[triangles[triIndex + 2]], localScale) + position;

            var tri = new Triangle(v1, v2, v3);

            // calculate the angle of this triangles resistance
            var cosAngle = Vec3.Dot(tri.Normal, -velocity) / (velocity.magnitude * tri.Normal.magnitude);
            var angle = Mathf.Acos(cosAngle);

            // magnitude of drag: 180 = 1, 135 = 0.5, < 90 = 0
            angleMags[index] = Mathf.Clamp((angle - Mathf.PI / 2) / (Mathf.PI / 2), 0, 1);

            var velSqu = Mathf.Pow(velocity.magnitude, 2);

            if (useSimpleDrag)
                dragForces[index] = -.5f * velSqu * tri.Area * (angleMags[index] * dragMulti) * Vec3.Normalize(velocity);
            else
                dragForces[index] = -.5f * velSqu * tri.Area * (angleMags[index] * dragMulti) * Vec3.Normalize(tri.Normal);

            midpoints[index] = tri.Midpoint;
        }
    }

    private void OnDisable()
    {
        _nTriangles.Dispose();
        _nVertices.Dispose();
    }
}
