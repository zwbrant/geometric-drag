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

            // calculate two side-vectors of triangle
            Vec3 v2v1 = v2 - v1;
            Vec3 v3v1 = v3 - v1;

            //
            var triArea = Vec3.Cross(v2v1, v3v1).magnitude * .5f;

            Vec3 triNormal = Vec3.Cross(v2v1, v3v1);
            triNormal = Vec3.Normalize(triNormal);

            Vec3 midPoint = (v1 + v2 + v3) / 3;

            // save triangle normal
            normals[i / 3] = triNormal;

            

            //Debug.DrawLine(pos, pos + dragVect);

            // calculate the angle of this triangles resistance

            var cosAngle = Vec3.Dot(triNormal, dragVect) / (dragVect.magnitude * triNormal.magnitude);
            var angle = Mathf.Acos(cosAngle);

            var dragMag = Mathf.Clamp((angle - Mathf.PI / 2) / (Mathf.PI / 2), 0, 1);

            //Debug.DrawLine(midPoint, midPoint + (surfNorm * triArea), Color.red);

            var c = Color.Lerp(Color.green, Color.red, dragMag);
            _colors[_mesh.triangles[i]] = c;
            _colors[_mesh.triangles[i + 1]] = c;
            _colors[_mesh.triangles[i + 2]] = c;

            var dragForce = -triNormal * triArea * dragMag * dragVect.magnitude * DragMulti;
                
            RBody.AddForceAtPosition(dragForce, midPoint);
            //Debug.DrawLine(midPoint, midPoint + dragForce * 20f, Color.yellow);

        }

        _mesh.colors = _colors;
    }
}
