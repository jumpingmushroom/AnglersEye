using System;

namespace AnglersEye.Core.Model
{
    /// <summary>A plain 3D vector, so the model stays free of UnityEngine.</summary>
    public struct Vec3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public float LengthXZ => (float)Math.Sqrt(X * X + Z * Z);
    }
}
