namespace Game.Core
{
    /// <summary>Hit-stop tuning (기획서 11.2, all TEMPORARY).</summary>
    public readonly struct HitStopConfig
    {
        /// <summary>Damage below this never freezes the game (B15).</summary>
        public readonly float DamageThreshold;

        /// <summary>Damage at which the freeze reaches MaxDuration.</summary>
        public readonly float ReferenceDamage;

        public readonly float MinDuration;
        public readonly float MaxDuration;

        public HitStopConfig(float damageThreshold, float referenceDamage, float minDuration, float maxDuration)
        {
            DamageThreshold = damageThreshold;
            ReferenceDamage = referenceDamage;
            MinDuration = minDuration;
            MaxDuration = maxDuration;
        }
    }

    /// <summary>
    /// Hit-stop timing rules. Kept engine-free so "weak hits do not freeze" and
    /// "simultaneous kills do not stack" are testable without entering play mode.
    /// </summary>
    public static class HitStopLogic
    {
        /// <summary>B15: freeze duration for a hit, or 0 when the hit is too weak.</summary>
        public static float DurationFor(float damage, in HitStopConfig config)
        {
            if (damage < config.DamageThreshold)
            {
                return 0f;
            }

            float span = config.ReferenceDamage - config.DamageThreshold;
            if (span <= 1e-6f)
            {
                return config.MaxDuration;
            }

            float t = (damage - config.DamageThreshold) / span;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;

            return config.MinDuration + (config.MaxDuration - config.MinDuration) * t;
        }

        /// <summary>
        /// B16: merging two freezes keeps the longer one — never the sum, so a
        /// multi-kill cannot lock the game for a noticeable stretch.
        /// </summary>
        public static float Merge(float remaining, float incoming)
        {
            return incoming > remaining ? incoming : remaining;
        }
    }
}
