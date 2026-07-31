namespace Game.Core
{
    /// <summary>Tuning for JUMP-001 (all TEMPORARY, owned by BallMovementConfig).</summary>
    public readonly struct JumpConfig
    {
        /// <summary>Grace period after leaving ground during which a jump is still allowed.</summary>
        public readonly float CoyoteTime;

        /// <summary>How long a jump press stays queued while airborne.</summary>
        public readonly float BufferTime;

        public JumpConfig(float coyoteTime, float bufferTime)
        {
            CoyoteTime = coyoteTime;
            BufferTime = bufferTime;
        }
    }

    /// <summary>Timers and the once-per-airtime latch for JUMP-001.</summary>
    public readonly struct JumpState
    {
        public readonly float TimeSinceGrounded;
        public readonly float TimeSincePressed;
        public readonly bool Consumed;

        public JumpState(float timeSinceGrounded, float timeSincePressed, bool consumed)
        {
            TimeSinceGrounded = timeSinceGrounded;
            TimeSincePressed = timeSincePressed;
            Consumed = consumed;
        }

        /// <summary>Airborne, never pressed, nothing consumed.</summary>
        public static JumpState Initial => new JumpState(Never, Never, false);

        internal const float Never = 999f;
    }

    /// <summary>
    /// JUMP-001: grounded jump with coyote time (B2), no double jump (B3) and
    /// input buffering (B4). Engine-free so every timing rule is EditMode-testable.
    /// </summary>
    public static class JumpLogic
    {
        /// <summary>Advances timers for one physics step. Landing clears the once-per-airtime latch.</summary>
        public static JumpState Step(JumpState state, bool isGrounded, bool pressedThisStep, float deltaTime)
        {
            float timeSinceGrounded = isGrounded ? 0f : state.TimeSinceGrounded + deltaTime;
            bool consumed = isGrounded ? false : state.Consumed;
            float timeSincePressed = pressedThisStep ? 0f : state.TimeSincePressed + deltaTime;

            return new JumpState(timeSinceGrounded, timeSincePressed, consumed);
        }

        /// <summary>True when a queued press may fire: within the coyote window and not yet used.</summary>
        public static bool ShouldJump(in JumpState state, in JumpConfig config)
        {
            if (state.Consumed)
            {
                return false;
            }

            return state.TimeSincePressed <= config.BufferTime
                   && state.TimeSinceGrounded <= config.CoyoteTime;
        }

        /// <summary>Marks the jump as used and clears the queued press.</summary>
        public static JumpState ConsumeJump(JumpState state)
        {
            return new JumpState(JumpState.Never, JumpState.Never, true);
        }

        /// <summary>
        /// Initial upward speed for a full up-and-down air time under the given
        /// gravity magnitude: v = g * t / 2.
        /// </summary>
        public static float JumpSpeedForAirTime(float airTime, float gravityMagnitude)
        {
            return gravityMagnitude * airTime * 0.5f;
        }
    }
}
