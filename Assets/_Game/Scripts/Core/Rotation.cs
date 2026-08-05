using System;

namespace Game.Core
{
    /// <summary>
    /// Rotation helpers shared by weapon attachment (WPN-001a, great circle) and
    /// projectile spread (WPN-009, about an axis). Keeping one implementation means
    /// the degenerate cases — coincident, antipodal and zero vectors — are handled once.
    /// </summary>
    public static class Rotation
    {
        /// <summary>Angle between two directions in degrees. A zero vector gives 0.</summary>
        public static float AngleDegrees(Float3 a, Float3 b)
        {
            Float3 na = a.Normalized();
            Float3 nb = b.Normalized();
            if (na.Equals(Float3.Zero) || nb.Equals(Float3.Zero))
            {
                return 0f;
            }

            float dot = Float3.Dot(na, nb);
            if (dot > 1f)
            {
                dot = 1f;
            }
            else if (dot < -1f)
            {
                dot = -1f;
            }

            return (float)(Math.Acos(dot) * 180.0 / Math.PI);
        }

        /// <summary>
        /// Rotates <paramref name="from"/> toward <paramref name="toward"/> along the
        /// great circle through both, by <b>exactly</b> <paramref name="degrees"/> —
        /// even when they already sit closer than that. WPN-001a's push-away needs
        /// this; aim assist wants <see cref="Towards"/> instead.
        /// </summary>
        public static Float3 Exactly(Float3 from, Float3 toward, float degrees)
        {
            Float3 start = from.Normalized();
            Float3 target = toward.Normalized();
            if (start.Equals(Float3.Zero))
            {
                return target;
            }

            // Tangent at `start`, pointing toward `target`.
            Float3 tangent = (target - start * Float3.Dot(target, start)).Normalized();
            if (tangent.Equals(Float3.Zero))
            {
                // Coincident or antipodal — no unique direction to travel, so any
                // perpendicular is as good as another.
                tangent = Perpendicular(start);
            }

            double radians = degrees * Math.PI / 180.0;
            return (start * (float)Math.Cos(radians) + tangent * (float)Math.Sin(radians)).Normalized();
        }

        /// <summary>Rodrigues rotation of <paramref name="v"/> around an axis (WPN-009 spread).</summary>
        public static Float3 AroundAxis(Float3 v, Float3 axis, float degrees)
        {
            Float3 k = axis.Normalized();
            if (k.Equals(Float3.Zero))
            {
                return v;
            }

            double radians = degrees * Math.PI / 180.0;
            var cos = (float)Math.Cos(radians);
            var sin = (float)Math.Sin(radians);

            return v * cos + Cross(k, v) * sin + k * (Float3.Dot(k, v) * (1f - cos));
        }

        public static Float3 Cross(Float3 a, Float3 b) => new Float3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

        /// <summary>Any unit vector perpendicular to <paramref name="v"/>.</summary>
        public static Float3 Perpendicular(Float3 v)
        {
            // Cross with whichever axis v is least aligned to, so the result is stable.
            Float3 axis = Math.Abs(v.Y) < 0.9f ? Float3.Up : new Float3(1f, 0f, 0f);
            return Cross(v, axis).Normalized();
        }
    }
}
