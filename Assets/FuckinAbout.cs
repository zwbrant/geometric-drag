using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;

[RequireComponent(typeof(MeshFilter))]
public class FuckinAbout : MonoBehaviour
{
    [Range(0f, 1f)]
    public float DragMulti = 1f;
    public bool DebugForceVectors = false;
    public bool DebugVelocityVector = false;


    protected Vec3[] normals;
    private Mesh _mesh;
    private Rigidbody _rBody;
    private Color[] _colors;


    // Start is called before the first frame update
    void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        _rBody = GetComponent<Rigidbody>();
        if (_rBody == null)
            _rBody = gameObject.AddComponent<Rigidbody>();

        normals = new Vec3[_mesh.triangles.Length / 3];
        _colors = new Color[_mesh.vertices.Length];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vec3 pos = transform.position;
        Vec3 dragVect = -_rBody.velocity;

        if (dragVect.magnitude <= 0)
            return;

        if (DebugVelocityVector)
            Debug.DrawLine(pos, pos + dragVect);

        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            Vec3 v1, v2, v3;
            v1 = transform.TransformPoint(_mesh.vertices[_mesh.triangles[i]]);
            v2 = transform.TransformPoint(_mesh.vertices[_mesh.triangles[i + 1]]);
            v3 = transform.TransformPoint(_mesh.vertices[_mesh.triangles[i + 2]]);

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

            UpdateDebugColors(i, surfAngleMag);
        }

        _mesh.colors = _colors;
    }

    private void UpdateDebugColors(int vertexIndex, float dragMag)
    {
        var c = Color.Lerp(Color.green, Color.red, dragMag);
        _colors[_mesh.triangles[vertexIndex]] = c;
        _colors[_mesh.triangles[vertexIndex + 1]] = c;
        _colors[_mesh.triangles[vertexIndex + 2]] = c;
    }
}
