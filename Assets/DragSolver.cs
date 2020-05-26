using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Zane;
using Vec3 = UnityEngine.Vector3;

public class DragSolver : MonoBehaviour
{
    public const float AirMass = 1f;

    [Range(0, 1f)]
    public float DragMulti = .1f;
    public bool EnableBurst = true;
    public int BatchSize = 32;
    public bool DebugObtuseness = true;
    public bool DebugForceVectors = false;
    public bool DebugVelocityVector = false;
    public bool UseSimpleDrag = false;
    [Header("References")]
    public MeshFilter MeshFilter;
    public Rigidbody Rbody;

    private Vec3[] _normals;
    private Mesh _mesh;
    private int[] _triIndices;
    private Color32[] _colors;

    // persistent
    private NativeArray<Triangle> _nLocalTris;
    private NativeArray<Vec3> _nVertices;
    // temp
    private NativeArray<Vec3> _nDragForces;
    private NativeArray<Vec3> _nMidpoints;
    private NativeArray<DragResult> _nDragResults;


    // Start is called before the first frame update
    void Start()
    {


        if (MeshFilter == null)
            _mesh = GetComponent<MeshFilter>().mesh;
        else
            _mesh = MeshFilter.mesh;
        if (Rbody == null)
            Rbody = GetComponent<Rigidbody>();
        if (Rbody == null)
            Rbody = gameObject.AddComponent<Rigidbody>();

        _triIndices = _mesh.triangles;
        _normals = new Vec3[_mesh.triangles.Length / 3];
        _colors = new Color32[_mesh.vertices.Length];

        _nVertices = new NativeArray<Vec3>(_mesh.vertices, Allocator.Persistent);
        _nLocalTris = new NativeArray<Triangle>(_mesh.triangles.Length / 3, Allocator.Persistent);

        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            Triangle tri = new Triangle(
                _mesh.vertices[_mesh.triangles[i]],
                _mesh.vertices[_mesh.triangles[i + 1]],
                _mesh.vertices[_mesh.triangles[i + 2]]);
            _nLocalTris[i / 3] = tri;
        }

    }

    private JobHandle _jHandle;
    private void Update()
    {
        if (!EnableBurst)
            return;

        _nDragForces = new NativeArray<Vec3>(_mesh.triangles.Length / 3, Allocator.TempJob);
        _nMidpoints = new NativeArray<Vec3>(_mesh.triangles.Length / 3, Allocator.TempJob);
        _nDragResults = new NativeArray<DragResult>(_mesh.triangles.Length / 3, Allocator.TempJob);


        var job = new DragUpdateJob() {
            vertices = _nVertices,
            localTris = _nLocalTris,
            dragForces = _nDragForces,
            midpoints = _nMidpoints,
            rotation = transform.rotation,
            position = transform.position,
            localScale = transform.localScale,
            airVelocity = -Rbody.velocity,
            dragMulti = DragMulti,
            useSimpleDrag = UseSimpleDrag,
             dragResults =_nDragResults
        };

        _jHandle = job.Schedule(_mesh.triangles.Length / 3, BatchSize);
    }

    private void LateUpdate()
    {
        if (!EnableBurst)
            return;

        _jHandle.Complete();

        for (int i = 0; i < _nLocalTris.Length; i++)
        {

            if (DebugObtuseness)
                UpdateDebugColors(i, _nDragResults[i].Obtuseness);

            if (float.IsNaN(_nDragResults[i].DragForce.x) || float.IsNaN(_nDragResults[i].ForceOrigin.x))
                continue;
            Rbody.AddForceAtPosition(_nDragResults[i].DragForce * DragMulti, _nDragResults[i].ForceOrigin);

            if (DebugForceVectors)
                Debug.DrawLine(_nDragResults[i].ForceOrigin, 
                    _nDragResults[i].ForceOrigin + _nDragResults[i].DragForce * DragMulti, Color.yellow);

        }

        if (DebugObtuseness)
            _mesh.colors32 = _colors;

        _nDragResults.Dispose();
        _nDragForces.Dispose();
        _nMidpoints.Dispose();
    }

    void FixedUpdate()
    {
        if (EnableBurst)
            return;

        Vec3 pos = transform.position;
        Vec3 dragVect = -Rbody.velocity;

        if (dragVect.magnitude <= 0)
            return;

        if (DebugVelocityVector)
            Debug.DrawLine(pos, pos + dragVect);

        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            Vec3 v1, v2, v3;
            v1 = transform.TransformPoint(_nVertices[_triIndices[i]]);
            v2 = transform.TransformPoint(_nVertices[_triIndices[i + 1]]);
            v3 = transform.TransformPoint(_nVertices[_triIndices[i + 2]]);

            var tri = new Triangle(v1, v2, v3);

            // save triangle normal
            _normals[i / 3] = tri.Normal;

            // calculate the angle of this triangles resistance
            var cosAngle = Vec3.Dot(tri.Normal, dragVect) / (dragVect.magnitude * tri.Normal.magnitude);
            var angle = Mathf.Acos(cosAngle);

            // magnitude of drag: 180 = 1, 135 = 0.5, < 90 = 0
            var surfAngleMag = Mathf.Clamp((angle - Mathf.PI / 2) / (Mathf.PI / 2), 0, 1);

            //Debug.DrawLine(midPoint, midPoint + (surfNorm * triArea), Color.red);

            var fluidDensity = 1f;
            var velSqu = Rbody.velocity.sqrMagnitude;

            var dragForce2 = -.5f * fluidDensity * velSqu * tri.Area * surfAngleMag * Vec3.Normalize(Rbody.velocity);

            //var dragForce = -tri.Normal * tri.Area * surfAngleMag * dragVect.magnitude * DragMulti;

            if (dragForce2.magnitude > 0f)
                Rbody.AddForceAtPosition(dragForce2, tri.Midpoint);

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
        _colors[_triIndices[index]] = c;
        _colors[_triIndices[index + 1]] = c;
        _colors[_triIndices[index + 2]] = c;
    }

    public struct DragUpdateJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Triangle> localTris;
        [ReadOnly]
        public NativeArray<Vec3> vertices;
        public Quaternion rotation;
        public Vec3 position;
        public Vec3 localScale;
        public Vec3 airVelocity;
        public float dragMulti;
        public bool useSimpleDrag;

        public NativeArray<DragResult> dragResults;
        public NativeArray<Vec3> dragForces;
        public NativeArray<Vec3> midpoints;

        public void Execute(int index)
        {
           Drag(index);
        }

        public void Drag(int i)
        {
            DragResult result;

            // convert local vertice positions to world
            var p1 = Math.TransformPoint(localTris[i].P1, position, rotation, localScale);
            var p2 = Math.TransformPoint(localTris[i].P2, position, rotation, localScale);
            var p3 = Math.TransformPoint(localTris[i].P3, position, rotation, localScale);
            var tri = new Triangle(p1, p2, p3);

            Vec3 windNorm = airVelocity / airVelocity.magnitude;


            // calculate the angle of this triangles resistance
            float cosTheta = Vec3.Dot(windNorm, tri.Normal) / (windNorm.magnitude * tri.Normal.magnitude);
            float projArea = Math.ProjectedTriArea(tri.vP1P2, tri.vP1P3, windNorm);

            cosTheta = Mathf.Clamp(cosTheta, -1, 0);

            // magnitude of drag: 180 = 1, 135 = 0.5, < 90 = 0
            result.Obtuseness = Mathf.Abs(cosTheta);

            //var velSq = Mathf.Pow(airVelocity.magnitude, 2);
            var velSq = Vec3.Dot(airVelocity, airVelocity);

            result.DragForce = AirMass * projArea * velSq * cosTheta * (1f + cosTheta / 2f) * tri.Normal;

            // result.DragForce = -.5f * velSq * tri.Area * (cosTheta * dragMulti) * Vec3.Normalize(tri.Normal);

            result.ForceOrigin = tri.Midpoint;

            dragResults[i] = result;
        }
    }

    public struct DragResult {
        public Vec3 DragForce;
        public Vec3 ForceOrigin;
        public float Obtuseness;
    }

    private void OnDisable()
    {
        _nLocalTris.Dispose();
        _nVertices.Dispose();
    }
}
