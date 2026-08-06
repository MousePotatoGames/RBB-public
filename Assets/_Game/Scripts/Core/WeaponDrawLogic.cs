namespace Game.Core
{
    /// <summary>
    /// WPN-005: which weapons a run hands out. Slot-count many, drawn without
    /// repeats from everything the game owns, in an order that changes run to run —
    /// that variety is the point (HYP-006 재도전 유도), so it is a rule, not a detail.
    /// Seeded so tests are deterministic.
    /// </summary>
    public static class WeaponDrawLogic
    {
        /// <summary>
        /// WPN-005 (2026-08-05 개정): draws <paramref name="pick"/> indices out of
        /// <paramref name="poolSize"/> without repeats. Once the pool is larger than
        /// the slot count, the *set* of weapons differs between runs — that is what
        /// HYP-006 (재도전 유도) actually rests on.
        /// </summary>
        public static int[] DrawIndices(int poolSize, int pick, int seed)
        {
            if (poolSize <= 0 || pick <= 0)
            {
                return new int[0];
            }

            var all = new int[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                all[i] = i;
            }

            Shuffle(all, seed);

            int take = pick < poolSize ? pick : poolSize;
            var result = new int[take];
            for (int i = 0; i < take; i++)
            {
                result[i] = all[i];
            }

            return result;
        }

        // Fisher-Yates with a small deterministic PRNG — Core has no UnityEngine.Random.
        private static void Shuffle<T>(T[] items, int seed)
        {
            uint state = (uint)seed * 747796405u + 2891336453u;
            for (int i = items.Length - 1; i > 0; i--)
            {
                state = state * 1664525u + 1013904223u;
                int j = (int)(state % (uint)(i + 1));
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
