using System;

namespace Game.Core
{
    /// <summary>The three First Playable weapons (WPN-005). F09~F11 branch on this.</summary>
    public enum WeaponKind
    {
        Spike = 0,
        Cannon = 1,
        Tesla = 2,
    }

    /// <summary>
    /// WPN-001 / WPN-001a: where a weapon ends up on the ball's surface.
    ///
    /// The contact point is used as-is — there are no predefined slots. Snapping
    /// to a fixed set would cap the number of possible attachment positions, and
    /// per-run variety is the point (it also decides the cannon's muzzle
    /// direction, CAN-001). Overlap is prevented by a minimum angular gap
    /// instead, which is all the silhouette control actually needs.
    /// </summary>
    public static class AttachmentLogic
    {
        /// <summary>
        /// B1~B6: the direction to attach at. Starts from the contact direction and,
        /// if it crowds an existing weapon, slides away along the surface until the
        /// minimum gap is met.
        /// </summary>
        /// <param name="contactDirection">Ball centre → contact point. Need not be normalised.</param>
        /// <param name="taken">Directions of already-attached weapons (unit vectors).</param>
        /// <param name="takenCount">How many entries of <paramref name="taken"/> are in use.</param>
        /// <param name="minSeparationDegrees">Minimum angle between any two weapons.</param>
        public static Float3 Resolve(Float3 contactDirection, Float3[] taken, int takenCount, float minSeparationDegrees)
        {
            Float3 dir = contactDirection.Normalized();
            if (dir.Equals(Float3.Zero))
            {
                // A contact exactly at the centre carries no direction. Refusing here
                // would make a pickup unclaimable, so fall back to "up".
                dir = Float3.Up;
            }

            if (taken == null || takenCount <= 0 || minSeparationDegrees <= 0f)
            {
                return dir;
            }

            float minDot = (float)Math.Cos(minSeparationDegrees * Math.PI / 180.0);

            // Each push can bring the direction closer to a *different* weapon, so
            // repeat until everything clears. Three weapons at 50° always converge;
            // the cap is a backstop, not a tuning knob.
            for (int pass = 0; pass < 8; pass++)
            {
                int crowded = NearestCrowding(dir, taken, takenCount, minDot);
                if (crowded < 0)
                {
                    return dir;
                }

                dir = PushAway(dir, taken[crowded], minSeparationDegrees);
            }

            return dir;
        }

        /// <summary>Index of the attached weapon this direction sits closest to, or -1 when clear.</summary>
        private static int NearestCrowding(Float3 dir, Float3[] taken, int takenCount, float minDot)
        {
            int worst = -1;
            float worstDot = minDot;

            for (int i = 0; i < takenCount; i++)
            {
                float dot = Float3.Dot(dir, taken[i]);
                if (dot > worstDot)
                {
                    worstDot = dot;
                    worst = i;
                }
            }

            return worst;
        }

        /// <summary>
        /// Rotates <paramref name="dir"/> directly away from <paramref name="from"/>
        /// along the great circle through both, until they are exactly
        /// <paramref name="degrees"/> apart.
        /// </summary>
        private static Float3 PushAway(Float3 dir, Float3 from, float degrees)
        {
            // Tangent pointing away from `from`, on the sphere at `dir`.
            Float3 away = (dir - from * Float3.Dot(dir, from)).Normalized();
            if (away.Equals(Float3.Zero))
            {
                // Exactly coincident (or antipodal): no unique "away", so pick any
                // perpendicular direction and rotate from `from` instead.
                away = Perpendicular(from);
                dir = from;
            }

            double radians = degrees * Math.PI / 180.0;
            return (from * (float)Math.Cos(radians) + away * (float)Math.Sin(radians)).Normalized();
        }

        /// <summary>Any unit vector perpendicular to <paramref name="v"/>.</summary>
        private static Float3 Perpendicular(Float3 v)
        {
            // Cross with whichever axis v is least aligned to, so the result is stable.
            Float3 axis = Math.Abs(v.Y) < 0.9f ? Float3.Up : new Float3(1f, 0f, 0f);
            var cross = new Float3(
                v.Y * axis.Z - v.Z * axis.Y,
                v.Z * axis.X - v.X * axis.Z,
                v.X * axis.Y - v.Y * axis.X);
            return cross.Normalized();
        }
    }
}
