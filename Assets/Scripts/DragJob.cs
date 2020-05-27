using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Zane;
using Vec3 = UnityEngine.Vector3;

public struct DragJob : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<DragResult> DragResults;
    [ReadOnly]
    public NativeArray<Triangle> LocalTris;
    [ReadOnly]
    public NativeArray<Vec3> AirForces;

    public Quaternion Rotation;
    public Vec3 Position;
    public Vec3 LocalScale;
    public float DragMulti;
    public bool UseSimpleDrag;

    // executed on each (local) triangle in the mesh 
    public void Execute(int index)
    {
        // convert local space triangle to world space
        var worldTri = TransformTriangle(index);

        // store the drag on this triangle for each given air force
        for (int i = 0; i < AirForces.Length; i++)
        {
            DragResults[index * AirForces.Length + i] = CalculateDrag(index, worldTri, AirForces[i]);
        }
    }

    public DragResult CalculateDrag(int index, Triangle tri, Vec3 airForce)
    {
        DragResult result;

        Vec3 windNorm = airForce / airForce.magnitude;

        // calculate the angle of this triangles resistance
        float cosTheta = Vec3.Dot(windNorm, tri.Normal) / (windNorm.magnitude * tri.Normal.magnitude);
        float projArea = Math.ProjectedTriArea(tri.vP1P2, tri.vP1P3, windNorm);

        cosTheta = Mathf.Clamp(cosTheta, -1, 0);

        // magnitude of drag: 180 = 1, 135 = 0.5, < 90 = 0
        result.Obtuseness = Mathf.Abs(cosTheta);

        //var velSq = Mathf.Pow(airVelocity.magnitude, 2);
        var velSq = Vec3.Dot(airForce, airForce);

        result.DragForce = DragSolver.AirMass * projArea * velSq * cosTheta * (1f + cosTheta / 2f) * tri.Normal;

        // result.DragForce = -.5f * velSq * tri.Area * (cosTheta * dragMulti) * Vec3.Normalize(tri.Normal);

        result.ForceOrigin = tri.Midpoint;

        return result;
    }

    private Triangle TransformTriangle(int index)
    {
        var p1 = Math.TransformPoint(LocalTris[index].P1, Position, Rotation, LocalScale);
        var p2 = Math.TransformPoint(LocalTris[index].P2, Position, Rotation, LocalScale);
        var p3 = Math.TransformPoint(LocalTris[index].P3, Position, Rotation, LocalScale);
        return new Triangle(p1, p2, p3);
    }
}

public struct DragResult
{
    public Vec3 DragForce;
    public Vec3 ForceOrigin;
    public float Obtuseness;
}