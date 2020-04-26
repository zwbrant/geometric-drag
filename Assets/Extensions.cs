using System;
using UnityEngine;

public static class Extensions
{
    public static Vector3 Midpoint(this Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return (v1 + v2 + v3) / 3;
    }

    public static Vector3 Area(this Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return (v1 + v2 + v3) / 3;
    }
}

public class Triangle
{
    public Vector3 P1 { get; private set; }
    public Vector3 P2 { get; private set; }
    public Vector3 P3 { get; private set; }

    public Vector3 vP2P1
    {
        get
        {
            if (_vP2P1 == null)
                _vP2P1 = P2 - P1;
            return _vP2P1;
        }
    }
    private Vector3 _vP2P1;
    public Vector3 vP3P1
    {
        get
        {
            if (_vP3P1 == null)
                _vP3P1 = P3 - P1;
            return _vP3P1;
        }
    }
    private Vector3 _vP3P1;

    public float Area
    {
        get
        {
            if (_area == -1f)
                _area = Vector3.Cross(vP2P1, vP3P1).magnitude * .5f;
            return _area;
        }
    }
    private float _area = -1f;

    public Vector3 Midpoint
    {
        get
        {
            if (_midpoint == null)
                _midpoint = (P1 + P2 + P3) / 3;
            return _midpoint;
        }
    }
    private Vector3 _midpoint;

    public Vector3 Normal
    {
        get
        {
            if (_normal == null)
            {
                _normal = Vector3.Cross(vP2P1, vP3P1);
                _normal = Vector3.Normalize(_normal);
            }
            return _normal;
        }
    }
    private Vector3 _normal;

    public Triangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
    }

}