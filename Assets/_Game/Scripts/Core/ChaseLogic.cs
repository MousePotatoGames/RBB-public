namespace Game.Core
{
    /// <summary>
    /// ENM-001: straight-line pursuit. Pure position stepping — the drone is a
    /// kinematic body (Decision 0001), so the logic owns the movement, not the
    /// physics solver.
    /// </summary>
    public static class ChaseLogic
    {
        private const float Epsilon = 1e-5f;

        /// <summary>Planar unit direction from a chaser to its target.</summary>
        public static Float3 DirectionTo(Float3 from, Float3 target)
        {
            Float3 delta = new Float3(target.X - from.X, 0f, target.Z - from.Z);
            return delta.Normalized();
        }

        /// <summary>
        /// B1: moves toward the target at <paramref name="speed"/>, never
        /// overshooting it even with a large delta time.
        /// </summary>
        public static Float3 Step(Float3 position, Float3 target, float speed, float deltaTime)
        {
            Float3 delta = new Float3(target.X - position.X, 0f, target.Z - position.Z);
            float distance = delta.Magnitude();
            if (distance < Epsilon)
            {
                return position;
            }

            float travel = speed * deltaTime;
            if (travel >= distance)
            {
                return new Float3(target.X, position.Y, target.Z);
            }

            Float3 step = delta / distance * travel;
            return new Float3(position.X + step.X, position.Y, position.Z + step.Z);
        }
    }
}
