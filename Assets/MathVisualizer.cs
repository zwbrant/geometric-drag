using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Linq;
using Zane;
using Vec3 = UnityEngine.Vector3;

public class MathVisualizer : MonoBehaviour
{
    [Range(-10, 10)]
    public float Test;
    public Vec3 TriX = new Vec3(0, 0, 0);
    public Vec3 TriY = new Vec3(3, 0, 0);
    public Vec3 Triz = new Vec3(0, 4, 0);
    public Vec3 Wind = new Vec3(.7f, -.5f, -.8f);
    Triangle _worldTri;

    Mesh _mesh;
    // Start is called before the first frame update
    void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        _mesh.name = "Cat";

        //var p1 = new Vec3(-2, 0, 0);
        //var p2 = new Vec3(2, 0, 0);
        //var p3 = new Vec3(-1, 1, 0);

        //_mesh.vertices = new Vec3[] { p1, p2, p3 };
        //_mesh.triangles = new int[] { 0, 2, 1 };
        //_mesh.RecalculateNormals();

        //_tri = new Triangle(p1, p2, p3);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTriangle();
        Debug.DrawLine(new Vec3(0, 0, 0), new Vec3(0, 0, 0) + Wind * 6f, Color.blue);
        Debug.DrawLine(new Vec3(0, 0, 0), new Vec3(0, 0, 0) - Wind * 6f, Color.blue);

     
        Vec3 vProject = Wind - (Vec3.Dot(Wind, _worldTri.Normal) / _worldTri.Normal.sqrMagnitude) * _worldTri.Normal;

        // figure out which points are closest and farthest from incoming wind vector
        var r = transform.position + vProject * 1000;
        var q = transform.position - vProject * 1000;

        var sortedPoints = SortByDist(_worldTri.P1, _worldTri.P2, _worldTri.P3, q);
        r = sortedPoints[0] + vProject * 1000;
        q = sortedPoints[0] - vProject * 1000;
        Vec3 closeLineP = GetClosestPointOnLine(q, r, sortedPoints[0]);
        Vec3 midLineP = GetClosestPointOnLine(q, r, sortedPoints[1]);
        Vec3 farLineP = GetClosestPointOnLine(q, r, sortedPoints[2]);


        Debug.DrawLine(sortedPoints[0], sortedPoints[0] + vProject * 3f, Color.red);
       // Debug.DrawLine(closeP, farLineP, Color.red);
        Debug.DrawLine(sortedPoints[0], closeLineP, Color.green);
        Debug.DrawLine(sortedPoints[2], farLineP, Color.green);

        integrateAxis = farLineP - closeLineP;
        var axisLength = integrateAxis.magnitude;

        // vector in direction of thickness
        var vh = Vec3.Normalize(sortedPoints[1] - midLineP);
        var ve = _worldTri.Incenter - _worldTri.Midpoint;
        var hAvg = Vec3.Dot(vh, ve);

        Debug.DrawLine(midLineP, midLineP + vh * hAvg, Color.magenta);

        // maybe? 
        var b = (GetClosestPointOnLine(closeLineP, farLineP, _worldTri.Midpoint) - closeLineP).magnitude;

        Debug.DrawLine(_worldTri.Midpoint, closeLineP + (Vec3.Normalize(integrateAxis) * b), Color.white);

        var S = Mathf.Sqrt(1 - (Mathf.Pow(b, 2) / Mathf.Pow(axisLength, 2)));

        var airNorm = Wind / Wind.magnitude;

        var p = projectedArea(_worldTri.vP1P2, _worldTri.vP1P3, airNorm);

        iAvg = IAvg(b, Test, S, axisLength);

        forceOrigin = O(sortedPoints[0], iAvg, vProject, hAvg, vh);

        //print("Mid: " + b + " / "  + axisLength);
        //print("Area: " + projectedArea(_worldTri.vP1P2, _worldTri.vP1P3, airNorm));
        //print("Avg: " + avgDist + " / " + axisLength);
        print("Avg Thick: " + hAvg);

        startPoint = sortedPoints[0];

        
    }

    Vec3 startPoint, integrateAxis, forceOrigin;
    float iAvg;
    public void OnDrawGizmos()
    {
        if (iAvg == 0 || startPoint == Vec3.zero)
            return;
        Gizmos.color = Color.blue;

        Gizmos.DrawSphere(startPoint + (Vec3.Normalize(integrateAxis) * iAvg), .05f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_worldTri.Incenter, .03f);
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(_worldTri.Midpoint, .03f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(forceOrigin, .03f);
        //Gizmos.DrawLine(_worldTri.Midpoint, startPoint + (Vec3.Normalize(integrateAxis) * avgDist));
    }

    public Vec3 GetClosestPointOnLine(Vec3 a, Vec3 b, Vec3 point)
    {
        float t = Vec3.Dot((b - a), (a - point)) / Vec3.Dot((b - a), (b - a));
        var x = a - t * (b - a);

        return x;
    }

    public List<Vec3> SortByDist(Vec3 p1, Vec3 p2, Vec3 p3, Vec3 otherPoint)
    {
        List<Vec3> result = new List<Vec3> { p1, p2, p3 };
        result = result.OrderBy(x => Vec3.Distance(x, otherPoint)).ToList<Vec3>();

        return result;
    }

    public float IAvg(float b, float p, float S, float L)
    {
        var top = (2f * b.Pow(3) * (p * (S - 2f) + 2f)) +
            (b * L.Pow(2) * (p * (4f - 5f * S) - 4f)) +
            (3f * L.Pow(2) * p * (b - L) * Mathf.Asin(b / L)) +
            (3f * b * L.Pow(2) * p * Mathf.Acos(b / L));

        var bottom = 4f * ((b.Pow(2) * (p * (S - 3f) + 3f)) +
            (b * L * (5f * p - 3f)) -
            (3f * b * L * p * Mathf.Acos(b / L)) +
            (2f * L.Pow(2) * p * (S - 1f)));

        return top / bottom;
    }

    public Vec3 O(Vec3 vA, float iAvg, Vec3 vProj, float hAvg, Vec3 vh )
    {
        return vA + (iAvg * (vProj / vProj.magnitude)) + (hAvg * vh); 

    }


    public float iEstimate(float b, float p, float S, float L)
    {
        return 1;
    }

        public float projectedArea(Vec3 a, Vec3 b, Vec3 airNorm)
    {
        return Vec3.Cross(a - airNorm * Vec3.Dot(a, airNorm), 
            b - airNorm * Vec3.Dot(b, airNorm)).magnitude * .5f;
    }

    public void UpdateTriangle()
    {
        _mesh.vertices = new Vec3[] { TriX, TriY, Triz };
        _mesh.triangles = new int[] { 0, 2, 1 };
        _mesh.RecalculateNormals();

        var p1 = transform.TransformPoint(TriX);
        var p2 = transform.TransformPoint(TriY);
        var p3 = transform.TransformPoint(Triz);
        _worldTri = new Triangle(p1, p2, p3);
    }
}
