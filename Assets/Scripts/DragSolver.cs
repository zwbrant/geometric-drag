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
    public static List<Vec3> GlobalAirForces { get; private set; }

    public List<Vec3> AirForces;
    [Range(0, 1f)]
    public float ForceDeadzone = 0.001f;
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
    // temp
    private NativeArray<Vec3> _nAirForces;
    private NativeArray<DragResult> _nDragResults;

    #region Unity Hooks
    void Start()
    {
        if (GlobalAirForces == null)
            GlobalAirForces = new List<Vec3>();

        if (MeshFilter == null)
            MeshFilter = GetComponent<MeshFilter>();
        _mesh = MeshFilter.mesh;

        if (Rbody == null)
            Rbody = GetComponent<Rigidbody>();
        if (Rbody == null)
            Rbody = gameObject.AddComponent<Rigidbody>();

        if (AirForces == null)
            AirForces = new List<Vec3>();

        _triIndices = _mesh.triangles;
        _normals = new Vec3[_mesh.triangles.Length / 3];
        _colors = new Color32[_mesh.vertices.Length];

        _nLocalTris = new NativeArray<Triangle>(_mesh.triangles.Length / 3, Allocator.Persistent);

        // populate custom array of triangle structures
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

        BuildAirForceArray();
        // psuedo-2d array: each tri gets a result for each air force
        _nDragResults = new NativeArray<DragResult>(_nLocalTris.Length * _nAirForces.Length, Allocator.TempJob);

        var job = new DragJob() {
            DragResults = _nDragResults,
            AirForces = _nAirForces,
            LocalTris = _nLocalTris,
            Rotation = transform.rotation,
            Position = transform.position,
            LocalScale = transform.localScale,
            DragMulti = DragMulti,
            UseSimpleDrag = UseSimpleDrag
        };

        _jHandle = job.Schedule(_nLocalTris.Length, BatchSize);
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

        for (int i = 0; i < _nLocalTris.Length; i++)
        {
            Vec3 v1, v2, v3;
            v1 = transform.TransformPoint(_nLocalTris[i].P1);
            v2 = transform.TransformPoint(_nLocalTris[i].P2);
            v3 = transform.TransformPoint(_nLocalTris[i].P3);

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

    private void LateUpdate()
    {
        if (!EnableBurst)
            return;

        _jHandle.Complete();

        // for each mesh tri, process each drag force that has acted on it
        for (int i = 0; i < _nLocalTris.Length; i++)
        {
            float obtuseness = 0f;
            for (int j = 0; j < _nAirForces.Length; j++)
            {
                var result = _nDragResults[i * _nAirForces.Length + j];
                if (!CheckValidity(result, ForceDeadzone))
                    continue;

                Rbody.AddForceAtPosition(result.DragForce * Time.deltaTime * DragMulti, result.ForceOrigin);

                obtuseness += result.Obtuseness;

                if (DebugForceVectors)
                    Debug.DrawLine(result.ForceOrigin, result.ForceOrigin + result.DragForce * DragMulti, Color.yellow);
            }
            UpdateDebugColors(i, obtuseness);
        }

        if (DebugObtuseness)
            _mesh.colors32 = _colors;

        _nAirForces.Dispose();
        _nDragResults.Dispose();
    }

    private void OnDisable()
    {
        _nLocalTris.Dispose();
    }
    #endregion


    public static int AddGlobalAirForce(Vec3 airForce)
    {
        if (GlobalAirForces == null)
            GlobalAirForces = new List<Vec3>();

        GlobalAirForces.Add(airForce);
        return GlobalAirForces.Count - 1;
    }

    public static void SetGlobalAirForce(int index, Vec3 value)
    {
        if (index < 0 || index >= GlobalAirForces.Count)
            throw new System.ArgumentOutOfRangeException();

        if (value.IsNan())
            return;

        GlobalAirForces[index] = value;
    }

    private void BuildAirForceArray()
    {
        // first index is reserved for velocity drag
        _nAirForces = new NativeArray<Vec3>(1 + AirForces.Count + GlobalAirForces.Count, Allocator.TempJob);
        // add velocity drag
        _nAirForces[0] = -Rbody.velocity;

        // add local forces from the public list
        for (int i = 1; i < 1 + AirForces.Count; i++)
            _nAirForces[i] = AirForces[i - 1];
        // add global forces
        for (int i = 1 + AirForces.Count; i < _nAirForces.Length; i++)
            _nAirForces[i] = GlobalAirForces[i - (1 + AirForces.Count)];
    }

    public static bool CheckValidity(DragResult result, float forceDeadzone)
    {
        if (result.ForceOrigin.IsNan() || result.DragForce.IsNan())
            return false;

        if (result.DragForce.magnitude <= forceDeadzone)
            return false;

        return true;
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

}
