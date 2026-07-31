namespace Game.Core
{
    /// <summary>Spawn budget tuning for the 90s First Playable curve (WAVE-001 축소판).</summary>
    public readonly struct SpawnBudgetConfig
    {
        public readonly int StartCount;
        public readonly int EndCount;
        public readonly float RampSeconds;
        public readonly int HardCap;
        public readonly float SpawnInterval;

        public SpawnBudgetConfig(int startCount, int endCount, float rampSeconds, int hardCap, float spawnInterval)
        {
            StartCount = startCount;
            EndCount = endCount;
            RampSeconds = rampSeconds;
            HardCap = hardCap;
            SpawnInterval = spawnInterval;
        }
    }

    /// <summary>
    /// WAVE-001: how many drones should be alive at a given moment, and whether
    /// one may spawn right now. Engine-free so the curve is EditMode-testable.
    /// </summary>
    public static class SpawnBudgetLogic
    {
        /// <summary>B3: linear ramp from StartCount to EndCount over RampSeconds, then held.</summary>
        public static int TargetCount(float elapsedSeconds, in SpawnBudgetConfig config)
        {
            if (elapsedSeconds <= 0f)
            {
                return config.StartCount;
            }

            if (config.RampSeconds <= 0f || elapsedSeconds >= config.RampSeconds)
            {
                return config.EndCount;
            }

            float t = elapsedSeconds / config.RampSeconds;
            float value = config.StartCount + (config.EndCount - config.StartCount) * t;
            return (int)System.Math.Round(value);
        }

        /// <summary>B4, B5: spawn only below target, respecting the interval and the hard cap.</summary>
        public static bool ShouldSpawn(int currentCount, int targetCount, float secondsSinceLastSpawn, in SpawnBudgetConfig config)
        {
            if (currentCount >= config.HardCap)
            {
                return false;
            }

            if (currentCount >= targetCount)
            {
                return false;
            }

            return secondsSinceLastSpawn >= config.SpawnInterval;
        }
    }
}
