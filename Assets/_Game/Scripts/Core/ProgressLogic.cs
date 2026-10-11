namespace Game.Core
{
    /// <summary>
    /// XP-002 (B7~B9): the experience curve.
    ///
    /// Thresholds are <b>cumulative</b> totals, exactly as XP-002 writes them
    /// (Lv2 = 15, Lv3 = 35, ...), so the caller never has to track "XP into this
    /// level" separately — total experience is the only state that matters.
    /// </summary>
    public static class ExperienceLogic
    {
        /// <summary>Everyone starts here. Level 1 needs no threshold.</summary>
        public const int FirstLevel = 1;

        /// <summary>
        /// B7/B9: the level for a total. Past the last threshold the level stops
        /// climbing — the curve is finite and XP-002 does not extrapolate it.
        /// </summary>
        public static int LevelFor(float totalExperience, float[] cumulativeThresholds)
        {
            if (cumulativeThresholds == null)
            {
                return FirstLevel;
            }

            int level = FirstLevel;
            for (int i = 0; i < cumulativeThresholds.Length; i++)
            {
                if (totalExperience < cumulativeThresholds[i])
                {
                    break;
                }

                level++;
            }

            return level;
        }

        /// <summary>
        /// B8: how many level-ups one experience gain produced. A single orb can
        /// cross two thresholds, and LVL-001 owes the player a card for each — so
        /// this returns a count rather than a bool.
        /// </summary>
        public static int PendingLevelUps(int levelBefore, int levelAfter)
        {
            int gained = levelAfter - levelBefore;
            return gained > 0 ? gained : 0;
        }

        /// <summary>
        /// Progress through the current level, 0..1. Returns 1 at the top of the
        /// curve. F14's HUD is the consumer; exposed here so the arithmetic stays
        /// in Core with the thresholds it belongs to.
        /// </summary>
        public static float Progress(float totalExperience, float[] cumulativeThresholds)
        {
            if (cumulativeThresholds == null || cumulativeThresholds.Length == 0)
            {
                return 1f;
            }

            int level = LevelFor(totalExperience, cumulativeThresholds);
            int nextIndex = level - FirstLevel;

            if (nextIndex >= cumulativeThresholds.Length)
            {
                return 1f; // B9: nothing left to fill
            }

            float floor = nextIndex == 0 ? 0f : cumulativeThresholds[nextIndex - 1];
            float ceiling = cumulativeThresholds[nextIndex];
            float span = ceiling - floor;

            if (span <= 0f)
            {
                return 1f;
            }

            float filled = (totalExperience - floor) / span;
            return filled < 0f ? 0f : (filled > 1f ? 1f : filled);
        }
    }

    /// <summary>
    /// XP-001 (B3~B5): orb attraction, by distance rather than a trigger volume.
    ///
    /// Same judgement as every other hit test in this project (F09 cone, F10
    /// sweep, F11 distance, F12 radius): no collider on the thing being tested.
    /// An orb with a trigger would also have to answer to the ball's Rigidbody,
    /// and a collider on the pickup path is how F08 nearly shoved the ball.
    /// </summary>
    public static class MagnetLogic
    {
        /// <summary>B3: inside the magnet radius, boundary included.</summary>
        public static bool InRange(Float3 orb, Float3 player, float radius) =>
            (player - orb).SqrMagnitude() <= radius * radius;

        /// <summary>
        /// B3: one step toward the player, never past them. Overshoot would make
        /// the orb oscillate around the ball instead of being swallowed by it.
        /// </summary>
        public static Float3 Step(Float3 orb, Float3 player, float speed, float deltaTime)
        {
            Float3 toPlayer = player - orb;
            float distance = toPlayer.Magnitude();
            float travel = speed * deltaTime;

            if (distance <= 1e-6f || travel >= distance)
            {
                return player;
            }

            return orb + toPlayer / distance * travel;
        }

        /// <summary>B4: close enough to be swallowed.</summary>
        public static bool IsAbsorbed(Float3 orb, Float3 player, float absorbDistance) =>
            (player - orb).SqrMagnitude() <= absorbDistance * absorbDistance;
    }
}
