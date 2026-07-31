namespace Game.Core
{
    /// <summary>HP-001/HP-002 tuning (TEMPORARY, owned by PlayerHealth).</summary>
    public readonly struct HealthConfig
    {
        public readonly float MaxHealth;

        /// <summary>Seconds of immunity after taking a hit (HP-001: 0.35~0.55).</summary>
        public readonly float InvulnerabilityDuration;

        public HealthConfig(float maxHealth, float invulnerabilityDuration)
        {
            MaxHealth = maxHealth;
            InvulnerabilityDuration = invulnerabilityDuration;
        }
    }

    /// <summary>Current HP and remaining invulnerability.</summary>
    public readonly struct HealthState
    {
        public readonly float Current;
        public readonly float InvulnerabilityRemaining;

        public HealthState(float current, float invulnerabilityRemaining)
        {
            Current = current;
            InvulnerabilityRemaining = invulnerabilityRemaining;
        }

        public bool IsAlive => Current > 0f;
        public bool IsInvulnerable => InvulnerabilityRemaining > 0f;

        public static HealthState Full(in HealthConfig config) => new HealthState(config.MaxHealth, 0f);
    }

    /// <summary>Outcome of one damage attempt, so callers know what to broadcast.</summary>
    public readonly struct DamageResult
    {
        public readonly HealthState State;
        public readonly bool Applied;
        public readonly bool JustDied;

        public DamageResult(HealthState state, bool applied, bool justDied)
        {
            State = state;
            Applied = applied;
            JustDied = justDied;
        }
    }

    /// <summary>
    /// HP-001: damage with an invulnerability window (B13~B16). Engine-free so
    /// the timing rules are EditMode-testable.
    /// </summary>
    public static class HealthLogic
    {
        /// <summary>Advances the invulnerability timer for one step.</summary>
        public static HealthState Tick(HealthState state, float deltaTime)
        {
            float remaining = state.InvulnerabilityRemaining - deltaTime;
            return new HealthState(state.Current, remaining < 0f ? 0f : remaining);
        }

        /// <summary>
        /// B13~B16: applies damage unless invulnerable or already dead. HP is
        /// clamped at 0, and JustDied is true only on the transition to 0.
        /// </summary>
        public static DamageResult ApplyDamage(HealthState state, float amount, in HealthConfig config)
        {
            if (!state.IsAlive || state.IsInvulnerable || amount <= 0f)
            {
                return new DamageResult(state, false, false);
            }

            float next = state.Current - amount;
            if (next < 0f)
            {
                next = 0f;
            }

            bool justDied = next <= 0f;
            var newState = new HealthState(next, config.InvulnerabilityDuration);
            return new DamageResult(newState, true, justDied);
        }
    }
}
