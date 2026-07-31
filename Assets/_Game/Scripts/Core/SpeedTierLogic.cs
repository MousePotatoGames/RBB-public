namespace Game.Core
{
    /// <summary>SPD-001 speed bands. Order matters: comparisons rely on it.</summary>
    public enum SpeedTier
    {
        Low = 0,
        Mid = 1,
        High = 2,
        Rumble = 3
    }

    /// <summary>Tier boundaries as a fraction of max speed (SPD-001, all TEMPORARY).</summary>
    public readonly struct SpeedTierConfig
    {
        public readonly float MidThreshold;
        public readonly float HighThreshold;
        public readonly float RumbleThreshold;

        /// <summary>How far below a boundary the ball must fall before dropping a tier (B3).</summary>
        public readonly float Hysteresis;

        public SpeedTierConfig(float midThreshold, float highThreshold, float rumbleThreshold, float hysteresis)
        {
            MidThreshold = midThreshold;
            HighThreshold = highThreshold;
            RumbleThreshold = rumbleThreshold;
            Hysteresis = hysteresis;
        }
    }

    /// <summary>
    /// SPD-001: maps planar speed to a tier. Rising is immediate; falling needs
    /// the speed to drop a full hysteresis band below the boundary so the tier
    /// does not flicker while cruising on a threshold (B3).
    /// </summary>
    public static class SpeedTierLogic
    {
        /// <summary>
        /// B1/B2/B5: dashing forces Rumble; otherwise the tier follows speed/maxSpeed,
        /// clamped at Rumble. Pass the previous tier so hysteresis can apply.
        /// </summary>
        public static SpeedTier Evaluate(float speed, float maxSpeed, bool isDashing, SpeedTier currentTier, in SpeedTierConfig config)
        {
            if (isDashing)
            {
                return SpeedTier.Rumble;
            }

            if (maxSpeed <= 1e-6f)
            {
                return SpeedTier.Low;
            }

            float ratio = speed / maxSpeed;
            SpeedTier raw = FromRatio(ratio, config);
            if (raw >= currentTier)
            {
                return raw;
            }

            // Descending: require an extra hysteresis margin before giving up the tier.
            SpeedTier descended = FromRatio(ratio + config.Hysteresis, config);
            return descended < currentTier ? descended : currentTier;
        }

        /// <summary>Raw band for a ratio, ignoring hysteresis and dash.</summary>
        public static SpeedTier FromRatio(float ratio, in SpeedTierConfig config)
        {
            if (ratio >= config.RumbleThreshold)
            {
                return SpeedTier.Rumble;
            }

            if (ratio >= config.HighThreshold)
            {
                return SpeedTier.High;
            }

            if (ratio >= config.MidThreshold)
            {
                return SpeedTier.Mid;
            }

            return SpeedTier.Low;
        }
    }
}
