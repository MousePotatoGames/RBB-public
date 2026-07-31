using System;

namespace Game.Core
{
    /// <summary>
    /// Minimal 3D vector for the engine-free Core assembly (noEngineReferences).
    /// Game.Gameplay adapters convert to/from UnityEngine.Vector3.
    /// </summary>
    public readonly struct Float3 : IEquatable<Float3>
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Float3 Zero => new Float3(0f, 0f, 0f);
        public static Float3 Up => new Float3(0f, 1f, 0f);

        public static Float3 operator +(Float3 a, Float3 b) => new Float3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Float3 operator -(Float3 a, Float3 b) => new Float3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Float3 operator *(Float3 a, float s) => new Float3(a.X * s, a.Y * s, a.Z * s);
        public static Float3 operator /(Float3 a, float s) => new Float3(a.X / s, a.Y / s, a.Z / s);

        public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public float SqrMagnitude() => X * X + Y * Y + Z * Z;

        public float Magnitude() => (float)Math.Sqrt(SqrMagnitude());

        /// <summary>Returns Zero for near-zero vectors instead of NaN.</summary>
        public Float3 Normalized()
        {
            float mag = Magnitude();
            return mag < 1e-6f ? Zero : this / mag;
        }

        /// <summary>Component of this vector on the plane defined by (unit) normal.</summary>
        public Float3 OnPlane(Float3 unitNormal) => this - unitNormal * Dot(this, unitNormal);

        public bool Equals(Float3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Float3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}
