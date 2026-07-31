namespace Game.Core
{
    /// <summary>
    /// Pure data for ball movement. Values come from the BallMovementConfig
    /// ScriptableObject in Game.Gameplay (MOVE-005, all TEMPORARY).
    /// F03 fields default to "no effect" so F01 call sites stay valid.
    /// </summary>
    public readonly struct MoveConfig
    {
        /// <summary>Base max planar speed in m/s (MOVE-005: 10~13).</summary>
        public readonly float MaxSpeed;

        /// <summary>Acceleration in m/s^2 (derived from time-to-max-speed).</summary>
        public readonly float Acceleration;

        /// <summary>Slope assist coefficient, 0 = off (MOVE-004).</summary>
        public readonly float SlopeAssist;

        /// <summary>Airborne acceleration multiplier, 1 = same as ground (MOVE-002).</summary>
        public readonly float AirControl;

        /// <summary>Turn-component multiplier at max speed, 1 = no damping (MOVE-003).</summary>
        public readonly float SteeringAtMaxSpeed;

        public MoveConfig(
            float maxSpeed,
            float acceleration,
            float slopeAssist,
            float airControl = 1f,
            float steeringAtMaxSpeed = 1f)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            SlopeAssist = slopeAssist;
            AirControl = airControl;
            SteeringAtMaxSpeed = steeringAtMaxSpeed;
        }
    }
}
