using System;
using UnityEngine;

namespace Zane
{
    public static class Math
    {
        public static float Sin(float deg)
        {
            return Mathf.Sin(Mathf.Deg2Rad);
        }

        public static float Cos(float deg)
        {
            return Mathf.Cos(Mathf.Deg2Rad);
        }

        public static float Tan(float deg)
        {
            return Mathf.Tan(Mathf.Deg2Rad);
        }

        public static float Asin(float deg)
        {
            return Mathf.Asin(Mathf.Deg2Rad);
        }

        public static float Acos(float deg)
        {
            return Mathf.Acos(Mathf.Deg2Rad);
        }

        public static float Atan(float deg)
        {
            return Mathf.Atan(Mathf.Deg2Rad);
        }

    }

    public class Triangle
    {
        public Vector3 P1 { get; private set; }
        public Vector3 P2 { get; private set; }
        public Vector3 P3 { get; private set; }

        public Vector3 vP1P2
        {
            get
            {
                if (_vP1P2 == null)
                    _vP1P2 = P2 - P1;
                return (Vector3)_vP1P2;
            }
        }
        private Vector3? _vP1P2 = null;

        public Vector3 vP1P3
        {
            get
            {
                if (_vP1P3 == null)
                    _vP1P3 = P3 - P1;
                return (Vector3)_vP1P3;
            }
        }
        private Vector3? _vP1P3 = null;

        public Vector3 vP2P3
        {
            get
            {
                if (_vP2P3 == null)
                    _vP2P3 = P3 - P2;
                return (Vector3)_vP2P3;
            }
        }
        private Vector3? _vP2P3 = null;

        public float Area
        {
            get
            {
                if (_area == -1f)
                    _area = Vector3.Cross(vP1P2, vP1P3).magnitude * .5f;
                return _area;
            }
        }
        private float _area = -1f;

        public Vector3 Midpoint
        {
            get
            {
                if (_midpoint == null)
                    _midpoint = (P1 + P2 + P3) / 3f;
                return (Vector3)_midpoint;
            }
        }
        private Vector3? _midpoint = null;

        public Vector3 Normal
        {
            get
            {
                if (_normal == null)
                {
                    _normal = Vector3.Cross(vP1P2, vP1P3);
                    _normal = Vector3.Normalize((Vector3)_normal);
                }
                return (Vector3)_normal;
            }
        }
        private Vector3? _normal = null;

        public Vector3 Incenter
        {
            get
            {
                if (_incenter == null)
                {
                    float sideLengthsSum = vP1P2.magnitude + vP2P3.magnitude + vP1P3.magnitude;
                    float p1Length = vP2P3.magnitude;
                    float p2Length = vP1P3.magnitude;
                    float p3Length = vP1P2.magnitude;

                    float x = ((P1.x * p1Length) + (P2.x * p2Length) + (P3.x * p3Length)) / sideLengthsSum;
                    float y = ((P1.y * p1Length) + (P2.y * p2Length) + (P3.y * p3Length)) / sideLengthsSum;
                    float z = ((P1.z * p1Length) + (P2.z * p2Length) + (P3.z * p3Length)) / sideLengthsSum;

                    _incenter = new Vector3(x, y, z);
                }
                return (Vector3)_incenter;
            }
        }
        private Vector3? _incenter = null;

        public Vector3 Eccentricity
        {
            get
            {
                if (_eccentricty == null)
                    _eccentricty = Incenter - Midpoint;
                return (Vector3)_eccentricty;
            }
        }
        private Vector3? _eccentricty = null;

        public Triangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

    }


}