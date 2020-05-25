using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vec3 = UnityEngine.Vector3;

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

        public static Vec3 TransformPoint(Vec3 point, Vec3 position, Quaternion rotation, Vec3 localScale)
        {
            return rotation * Vec3.Scale(point, localScale) + position;

        }

        public static float ProjectedTriArea(Vec3 sideA, Vec3 sideB, Vec3 windNorm)
        {
            return Vec3.Cross(sideA - windNorm * Vec3.Dot(sideA, windNorm),
                sideB - windNorm * Vec3.Dot(sideB, windNorm)).magnitude * .5f;
        }

        public static Vec3 GetClosestPointOnLine(Vec3 a, Vec3 b, Vec3 point)
        {
            float t = Vec3.Dot((b - a), (a - point)) / Vec3.Dot((b - a), (b - a));
            var x = a - t * (b - a);

            return x;
        }

        public static List<Vec3> SortByDist(Vec3 p1, Vec3 p2, Vec3 p3, Vec3 otherPoint)
        {
            List<Vec3> result = new List<Vec3> { p1, p2, p3 };
            result = result.OrderBy(x => Vec3.Distance(x, otherPoint)).ToList<Vec3>();

            return result;
        }

        public static float IAvg(float b, float p, float S, float L)
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

        public static Vec3 ForceOrigin(Vec3 vA, float iAvg, Vec3 vProj, float hAvg, Vec3 vh)
        {
            return vA + (iAvg * (vProj / vProj.magnitude)) + (hAvg * vh);

        }
    }

    public struct Triangle
    {
        public Vec3 P1 { get; private set; }
        public Vec3 P2 { get; private set; }
        public Vec3 P3 { get; private set; }

        public Vec3 vP1P2
        {
            get
            {
                if (_vP1P2 == default)
                    _vP1P2 = P2 - P1;
                return _vP1P2;
            }
        }
        private Vec3 _vP1P2;

        public Vec3 vP1P3
        {
            get
            {
                if (_vP1P3 == default)
                    _vP1P3 = P3 - P1;
                return _vP1P3;
            }
        }
        private Vec3 _vP1P3;

        public Vec3 vP2P3
        {
            get
            {
                if (_vP2P3 == default)
                    _vP2P3 = P3 - P2;
                return _vP2P3;
            }
        }
        private Vec3 _vP2P3;

        public float Area
        {
            get
            {
                if (_area == default)
                    _area = Vec3.Cross(vP1P2, vP1P3).magnitude * .5f;
                return _area;
            }
        }
        private float _area;

        public Vec3 Midpoint
        {
            get
            {
                if (_midpoint == default)
                    _midpoint = (P1 + P2 + P3) / 3f;
                return _midpoint;
            }
        }
        private Vec3 _midpoint;

        public Vec3 Normal
        {
            get
            {
                if (_normal == default)
                {
                    _normal = Vec3.Cross(vP1P2, vP1P3);
                    _normal = Vec3.Normalize((Vec3)_normal);
                }
                return _normal;
            }
        }
        private Vec3 _normal;

        public Vec3 Incenter
        {
            get
            {
                if (_incenter == default)
                {
                    float sideLengthsSum = vP1P2.magnitude + vP2P3.magnitude + vP1P3.magnitude;
                    float p1Length = vP2P3.magnitude;
                    float p2Length = vP1P3.magnitude;
                    float p3Length = vP1P2.magnitude;

                    float x = ((P1.x * p1Length) + (P2.x * p2Length) + (P3.x * p3Length)) / sideLengthsSum;
                    float y = ((P1.y * p1Length) + (P2.y * p2Length) + (P3.y * p3Length)) / sideLengthsSum;
                    float z = ((P1.z * p1Length) + (P2.z * p2Length) + (P3.z * p3Length)) / sideLengthsSum;

                    _incenter = new Vec3(x, y, z);
                }
                return _incenter;
            }
        }
        private Vec3 _incenter;

        public Vec3 Eccentricity
        {
            get
            {
                if (_eccentricty == default)
                    _eccentricty = Incenter - Midpoint;
                return _eccentricty;
            }
        }
        private Vec3 _eccentricty;

        public Triangle(Vec3 p1, Vec3 p2, Vec3 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            _eccentricty = default;
            _incenter = default;
            _normal = default;
            _midpoint = default;
            _area = default;
            _vP1P2 = default;
            _vP1P3 = default;
            _vP2P3 = default;
        }

    }


}