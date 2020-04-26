using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;

public class FuckinAbout : MonoBehaviour
{
    public MeshFilter MeshFilter;
    public Rigidbody RBody;
    [Range(0f, 1f)]
    public float DragMulti = 1f;

    protected Vec3[] normals;
    private Mesh _mesh;
    private Color[] _colors;


    // Start is called before the first frame update
    void Start()
    {
        _mesh = MeshFilter.mesh;
        normals = new Vec3[_mesh.triangles.Length / 3];
        _colors = new Color[_mesh.vertices.Length];

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vec3 pos = transform.position;
        Vec3 dragVect = -RBody.velocity;
        //dragVect = new Vector3(1, 0, 0);

        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            Vec3 v1, v2, v3;
            v1 = transform.TransformPoint(_mesh.vertices[_mesh.triangles[i]]);
            v2 = transform.TransformPoint(_mesh.vertices[_mesh.triangles[i + 1]]);
            v3 = transform.TransformPoint(_mesh.vertices[_mesh.triangles[i + 2]]);

            var tri = new Triangle(v1, v2, v3);

            // save triangle normal
            normals[i / 3] = tri.Normal;

          
            //Debug.DrawLine(pos, pos + dragVect);

            // calculate the angle of this triangles resistance
            var cosAngle = Vec3.Dot(tri.Normal, dragVect) / (dragVect.magnitude * tri.Normal.magnitude);
            var angle = Mathf.Acos(cosAngle);

            // magnitude of drag: 180 = 1, 135 = 0.5, < 90 = 0
            var dragMag = Mathf.Clamp((angle - Mathf.PI / 2) / (Mathf.PI / 2), 0, 1);

            //Debug.DrawLine(midPoint, midPoint + (surfNorm * triArea), Color.red);

            var dragForce = -tri.Normal * tri.Area * dragMag * dragVect.magnitude * DragMulti;
                
            if (dragForce.magnitude > 0f)
                RBody.AddForceAtPosition(dragForce, tri.Midpoint);
            
            //Debug.DrawLine(midPoint, midPoint + dragForce * 20f, Color.yellow);

            UpdateDebugColors(i, dragMag);
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
