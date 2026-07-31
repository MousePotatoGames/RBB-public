namespace Game.Core
{
    /// <summary>Tuning for DASH-001 (all TEMPORARY, owned by BallMovementConfig).</summary>
    public readonly struct DashConfig
    {
        /// <summary>Planar speed added by one dash, in m/s (MOVE-005: 7~10).</summary>
        public readonly float DashSpeed;

        /// <summary>Seconds before the dash is available again (MOVE-005: 1.5~2.2).</summary>
        public readonly float Cooldown;

        public DashConfig(float dashSpeed, float cooldown)
        {
            DashSpeed = dashSpeed;
            Cooldown = cooldown;
        }
    }

    /// <summary>Remaining cooldown for DASH-001.</summary>
    public readonly struct DashState
    {
        public readonly float CooldownRemaining;

        public DashState(float cooldownRemaining)
        {
            CooldownRemaining = cooldownRemaining;
        }

        /// <summary>Ready to dash.</summary>
        public static DashState Initial => new DashState(0f);
    }

    /// <summary>
    /// DASH-001: adds speed along the current input direction instead of
    /// teleporting (B5), and refuses to fire while cooling down (B6).
    /// </summary>
    public static class DashLogic
    {
        private const float Epsilon = 1e-4f;

        /// <summary>Advances the cooldown for one physics step.</summary>
        public static DashState Step(DashState state, float deltaTime)
        {
            float remaining = state.CooldownRemaining - deltaTime;
            return new DashState(remaining < 0f ? 0f : remaining);
        }

        public static bool IsReady(in DashState state) => state.CooldownRemaining <= 0f;

        /// <summary>
        /// Velocity change for a dash: along the move input, or along the current
        /// heading when there is no input. Zero when the ball has neither — the
        /// caller should then skip the dash and keep it off cooldown.
        /// </summary>
        public static Float3 DashVelocityChange(Float3 velocity, Float3 moveDirection, in DashConfig config)
        {
            Float3 direction = moveDirection;
            if (direction.Magnitude() < Epsilon)
            {
                direction = new Float3(velocity.X, 0f, velocity.Z);
            }

            Float3 unit = direction.Normalized();
            if (unit.Equals(Float3.Zero))
            {
                return Float3.Zero;
            }

            return unit * config.DashSpeed;
        }

        public static DashState StartCooldown(DashState state, in DashConfig config)
        {
            return new DashState(config.Cooldown);
        }
    }
}
