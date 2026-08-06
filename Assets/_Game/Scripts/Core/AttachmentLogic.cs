using System;

namespace Game.Core
{
    /// <summary>
    /// WPN-005 draw identity — the same kind appears at most once per run.
    ///
    /// Five kinds against three slots is what finally makes the draw vary: with
    /// three of three, only the order changed and every run handed out the same set.
    /// </summary>
    public enum WeaponKind
    {
        Spike = 0,
        Cannon = 1,
        Tesla = 2,
        Axe = 3,
        Pet = 4,
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

                // Rotate away from the crowding weapon: start there and travel toward
                // `dir` by exactly the minimum gap (F10 shares this with aim assist).
                dir = Rotation.Exactly(taken[crowded], dir, minSeparationDegrees);
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
    }
}
