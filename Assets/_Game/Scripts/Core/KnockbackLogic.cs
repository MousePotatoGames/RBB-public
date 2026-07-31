namespace Game.Core
{
    /// <summary>
    /// ENM-004 / DMG-003: how hard a hit enemy flies. Resistance lets heavy
    /// enemies (brute, boss) be pushed without being launched.
    /// </summary>
    public static class KnockbackLogic
    {
        /// <summary>
        /// B8/B9: impulse away from the player, scaled by how hard the hit was and
        /// reduced by the target's resistance (0 = flies freely, 1 = immovable).
        /// </summary>
        public static Float3 Impulse(Float3 playerToEnemy, float damage, float referenceDamage, float baseForce, float resistance)
        {
            Float3 direction = new Float3(playerToEnemy.X, 0f, playerToEnemy.Z).Normalized();
            if (direction.Equals(Float3.Zero) || baseForce <= 0f)
            {
                return Float3.Zero;
            }

            float clampedResistance = resistance < 0f ? 0f : (resistance > 1f ? 1f : resistance);
            float scale = referenceDamage <= 1e-6f ? 1f : damage / referenceDamage;
            if (scale > 1f)
            {
                scale = 1f;
            }

            float magnitude = baseForce * scale * (1f - clampedResistance);
            return direction * magnitude;
        }
    }
}
