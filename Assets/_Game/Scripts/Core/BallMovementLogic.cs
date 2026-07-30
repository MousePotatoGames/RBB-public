namespace Game.Core
{
    /// <summary>
    /// Engine-free ball movement rules. Implements MOVE-001 (camera-relative
    /// movement with inertia), MOVE-004 (slope assist) and the acceleration /
    /// max-speed contract of MOVE-005. BallMotor (Game.Gameplay) is the humble
    /// adapter that feeds this from the Rigidbody and applies the results.
    /// </summary>
    public static class BallMovementLogic
    {
        private const float InputEpsilon = 1e-4f;

        /// <summary>
        /// MOVE-001 (B1, B2, B7): converts 2D input into a world-plane direction
        /// using the camera's yaw basis. Input magnitude above 1 is normalized so
        /// diagonals are not faster; magnitude below 1 (analog) is preserved.
        /// </summary>
        public static Float3 CameraRelativeDirection(Float3 cameraForward, Float3 cameraRight, float inputX, float inputY)
        {
            Float3 forwardFlat = new Float3(cameraForward.X, 0f, cameraForward.Z).Normalized();
            Float3 rightFlat = new Float3(cameraRight.X, 0f, cameraRight.Z).Normalized();

            Float3 direction = rightFlat * inputX + forwardFlat * inputY;
            float magnitude = direction.Magnitude();
            if (magnitude > 1f)
            {
                direction = direction / magnitude;
            }

            return direction;
        }

        /// <summary>
        /// MOVE-001 / MOVE-005 (B3, B4, B5): velocity change for one physics step.
        /// - No input → Zero (no artificial braking; inertia is preserved).
        /// - Acceleration is finite (config.Acceleration).
        /// - Never pushes the on-plane speed beyond MaxSpeed, but never brakes
        ///   a body that is already faster (dash/knockback stay untouched).
        /// </summary>
        public static Float3 ComputeVelocityChange(Float3 velocity, Float3 moveDirection, Float3 groundNormal, in MoveConfig config, float deltaTime)
        {
            float inputMagnitude = moveDirection.Magnitude();
            if (inputMagnitude < InputEpsilon)
            {
                return Float3.Zero;
            }

            Float3 unitNormal = groundNormal.Normalized();
            if (unitNormal.Equals(Float3.Zero))
            {
                unitNormal = Float3.Up;
            }

            Float3 velocityOnPlane = velocity.OnPlane(unitNormal);
            Float3 directionNorm = moveDirection / inputMagnitude;

            float step = config.Acceleration * deltaTime * inputMagnitude;
            Float3 candidate = velocityOnPlane + directionNorm * step;

            float candidateSpeed = candidate.Magnitude();
            float allowedSpeed = System.Math.Max(config.MaxSpeed, velocityOnPlane.Magnitude());
            if (candidateSpeed > allowedSpeed && candidateSpeed > 1e-6f)
            {
                candidate = candidate * (allowedSpeed / candidateSpeed);
            }

            return candidate - velocityOnPlane;
        }

        /// <summary>
        /// MOVE-004 (B6): re-aims a flat movement direction along the ground
        /// slope, preserving the input magnitude so climbing does not slow the
        /// intent vector itself.
        /// </summary>
        public static Float3 ProjectOnSlope(Float3 direction, Float3 groundNormal)
        {
            float magnitude = direction.Magnitude();
            if (magnitude < InputEpsilon)
            {
                return Float3.Zero;
            }

            Float3 unitNormal = groundNormal.Normalized();
            Float3 projected = direction.OnPlane(unitNormal);
            float projectedMagnitude = projected.Magnitude();
            if (projectedMagnitude < 1e-6f)
            {
                return Float3.Zero;
            }

            return projected * (magnitude / projectedMagnitude);
        }

        /// <summary>
        /// MOVE-004 (B6): counter-force against the gravity component that acts
        /// along the slope plane, scaled by config.SlopeAssist. Zero on flat
        /// ground (gravity is parallel to the normal there).
        /// </summary>
        public static Float3 SlopeAssistForce(Float3 gravity, Float3 groundNormal, float slopeAssist)
        {
            Float3 unitNormal = groundNormal.Normalized();
            Float3 gravityAlongSlope = gravity.OnPlane(unitNormal);
            return gravityAlongSlope * (-slopeAssist);
        }
    }
}
