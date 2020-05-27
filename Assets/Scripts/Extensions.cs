using System;
using UnityEngine;

namespace Zane
{
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

        public static bool IsNan(this Vector3 v1)
        {
            return float.IsNaN(v1.x) || float.IsNaN(v1.y) || float.IsNaN(v1.z);
        }

        public static float Pow(this float f, int pow)
        {
            return Mathf.Pow(f, pow);
        }
    }
}