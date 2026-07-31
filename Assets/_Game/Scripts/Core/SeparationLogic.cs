namespace Game.Core
{
    /// <summary>
    /// ENM-004: keeps a swarm from collapsing into one blob without letting it
    /// scatter. Runs on positions only — enemies are kinematic (Decision 0001),
    /// so no physics solver is involved.
    /// </summary>
    public static class SeparationLogic
    {
        private const float Epsilon = 1e-5f;

        /// <summary>
        /// B8: push away from neighbours closer than <paramref name="minDistance"/>.
        /// The push grows as the overlap grows and is Zero when nothing is too close.
        /// </summary>
        public static Float3 Push(Float3 self, Float3[] neighbours, int neighbourCount, float minDistance)
        {
            if (neighbours == null || neighbourCount <= 0 || minDistance <= 0f)
            {
                return Float3.Zero;
            }

            Float3 accumulated = Float3.Zero;
            int contributors = 0;

            for (int i = 0; i < neighbourCount && i < neighbours.Length; i++)
            {
                Float3 delta = new Float3(self.X - neighbours[i].X, 0f, self.Z - neighbours[i].Z);
                float distance = delta.Magnitude();
                if (distance >= minDistance)
                {
                    continue;
                }

                Float3 away = distance < Epsilon
                    ? new Float3(1f, 0f, 0f) // exactly overlapping: pick a stable axis
                    : delta / distance;

                float overlap = (minDistance - distance) / minDistance; // 0..1
                accumulated += away * overlap;
                contributors++;
            }

            if (contributors == 0)
            {
                return Float3.Zero;
            }

            return accumulated / contributors;
        }

        /// <summary>
        /// B9: separation must stay weaker than pursuit so the swarm still closes in.
        /// Returns the separation velocity, capped at <paramref name="strengthRatio"/>
        /// of the chase speed.
        /// </summary>
        public static Float3 SeparationVelocity(Float3 push, float chaseSpeed, float strengthRatio)
        {
            float magnitude = push.Magnitude();
            if (magnitude < Epsilon)
            {
                return Float3.Zero;
            }

            float maxSpeed = chaseSpeed * strengthRatio;
            float speed = magnitude * maxSpeed;
            if (speed > maxSpeed)
            {
                speed = maxSpeed;
            }

            return push / magnitude * speed;
        }
    }
}
