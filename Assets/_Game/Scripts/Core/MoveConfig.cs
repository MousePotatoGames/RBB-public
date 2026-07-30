namespace Game.Core
{
    /// <summary>
    /// Pure data for ball movement. Values come from the BallMovementConfig
    /// ScriptableObject in Game.Gameplay (MOVE-005, all TEMPORARY).
    /// </summary>
    public readonly struct MoveConfig
    {
        /// <summary>Base max planar speed in m/s (MOVE-005: 10~13).</summary>
        public readonly float MaxSpeed;

        /// <summary>Acceleration in m/s^2 (derived from time-to-max-speed).</summary>
        public readonly float Acceleration;

        /// <summary>Slope assist coefficient, 0 = off (MOVE-004).</summary>
        public readonly float SlopeAssist;

        public MoveConfig(float maxSpeed, float acceleration, float slopeAssist)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            SlopeAssist = slopeAssist;
        }
    }
}
