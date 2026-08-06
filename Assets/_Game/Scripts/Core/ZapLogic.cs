using System;

namespace Game.Core
{
    /// <summary>TES-001 tuning for a zap weapon (all TEMPORARY).</summary>
    public readonly struct ZapConfig
    {
        /// <summary>Seconds between discharges.</summary>
        public readonly float Interval;

        /// <summary>
        /// Reach from the ring's attachment point. Short on purpose — a long reach
        /// would blur the line with the cannon and flatten the distance falloff.
        /// </summary>
        public readonly float Range;

        /// <summary>Damage at zero distance.</summary>
        public readonly float MaxDamage;

        /// <summary>Damage at the edge of the range.</summary>
        public readonly float MinDamage;

        public ZapConfig(float interval, float range, float maxDamage, float minDamage)
        {
            Interval = interval;
            Range = range;
            MaxDamage = maxDamage;
            MinDamage = minDamage;
        }
    }

    /// <summary>
    /// TES-001 / WPN-008: the tesla ring's discharge.
    ///
    /// No aiming and no projectile — it picks the closest enemy inside its reach and
    /// damages it immediately. What makes it a distinct weapon rather than a shorter
    /// cannon is <see cref="Damage"/>: being right on top of an enemy is worth
    /// several times more than clipping the edge of the range, so diving into a
    /// crowd is the correct play.
    ///
    /// Callers pass in positions rather than the logic querying for them — the
    /// spawner already tracks every live drone, so no physics query happens at all
    /// (TES-001 asks for one only at attack time; this is stricter).
    /// </summary>
    public static class ZapLogic
    {
        /// <summary>Sentinel for "has not discharged yet", so the first zap is never gated.</summary>
        public const float NeverZapped = float.NegativeInfinity;

        public const int NoTarget = -1;

        /// <summary>B1: has the discharge interval elapsed?</summary>
        public static bool CanZap(float lastZapTime, float now, float interval)
        {
            if (interval <= 0f)
            {
                return true;
            }

            return now - lastZapTime >= interval;
        }

        /// <summary>
        /// B2/B3: index of the closest position within <paramref name="range"/>, and how
        /// far away it is. Returns <see cref="NoTarget"/> when the range is empty, in
        /// which case <paramref name="distance"/> is 0.
        ///
        /// Only the first <paramref name="count"/> entries are read — a pooled list
        /// reuses its array, so anything past the live count is a recycled enemy.
        /// </summary>
        public static int NearestInRange(Float3 origin, Float3[] positions, int count, float range, out float distance)
        {
            distance = 0f;

            if (positions == null || count <= 0 || range <= 0f)
            {
                return NoTarget;
            }

            if (count > positions.Length)
            {
                count = positions.Length;
            }

            float bestSqr = range * range; // distance exactly at the range counts as in range
            int best = NoTarget;

            for (int i = 0; i < count; i++)
            {
                float sqr = (positions[i] - origin).SqrMagnitude();
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            if (best != NoTarget)
            {
                distance = (float)Math.Sqrt(bestSqr);
            }

            return best;
        }

        /// <summary>
        /// B4: damage falls off linearly with distance — <see cref="ZapConfig.MaxDamage"/>
        /// on contact down to <see cref="ZapConfig.MinDamage"/> at the edge. This
        /// gradient is the weapon's identity; without it the tesla is just a cannon
        /// that cannot miss.
        /// </summary>
        public static float Damage(float distance, in ZapConfig config)
        {
            if (config.Range <= 0f)
            {
                return config.MaxDamage;
            }

            float t = distance / config.Range;
            if (t < 0f)
            {
                t = 0f;
            }
            else if (t > 1f)
            {
                t = 1f; // past the range the caller should not have fired, but never go below the floor
            }

            return config.MaxDamage + (config.MinDamage - config.MaxDamage) * t;
        }
    }
}
