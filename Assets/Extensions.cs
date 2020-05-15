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

        public static float Pow(this float f, int pow)
        {
            return Mathf.Pow(f, pow);
        }
    }
}