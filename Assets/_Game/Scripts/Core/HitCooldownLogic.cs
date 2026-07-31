namespace Game.Core
{
    /// <summary>
    /// DMG-002: one collision must deal damage once, not once per physics frame.
    /// Tracked per enemy instance, so the rule is "this enemy cannot be hit again
    /// for N seconds" rather than a global attack cooldown.
    /// </summary>
    public static class HitCooldownLogic
    {
        /// <summary>Sentinel meaning "never hit yet".</summary>
        public const float NeverHit = float.NegativeInfinity;

        /// <summary>B6/B7: true when the cooldown since the last hit has elapsed.</summary>
        public static bool CanHit(float lastHitTime, float now, float cooldown)
        {
            if (float.IsNegativeInfinity(lastHitTime))
            {
                return true;
            }

            return now - lastHitTime >= cooldown;
        }
    }
}
