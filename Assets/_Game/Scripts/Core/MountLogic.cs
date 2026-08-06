using System;

namespace Game.Core
{
    /// <summary>
    /// WPN-007 (B1~B3): a weapon that circles the player instead of riding the ball.
    ///
    /// The ball's own rotation is deliberately absent from every signature here — an
    /// orbit weapon keeps its own motion no matter how the ball rolls, and that
    /// independence is the mount's identity.
    /// </summary>
    public static class OrbitLogic
    {
        /// <summary>B3: advances the orbit angle, wrapped to [0, 360).</summary>
        public static float Advance(float angleDegrees, float angularSpeedDegrees, float deltaTime)
        {
            float next = angleDegrees + angularSpeedDegrees * deltaTime;

            next %= 360f;
            if (next < 0f)
            {
                next += 360f;
            }

            return next;
        }

        /// <summary>
        /// B1: position on the circle around <paramref name="centre"/>. The centre is
        /// passed every call rather than cached, so the orbit tracks a moving player
        /// without anyone having to push updates at it.
        /// </summary>
        public static Float3 Position(Float3 centre, float angleDegrees, float radius, float height)
        {
            double radians = angleDegrees * Math.PI / 180.0;

            return new Float3(
                centre.X + (float)Math.Cos(radians) * radius,
                centre.Y + height,
                centre.Z + (float)Math.Sin(radians) * radius);
        }
    }

    /// <summary>
    /// WPN-007 (B4~B6): a weapon that trails the player like a pet.
    ///
    /// The lag is exponential rather than a fixed step, so the result depends on
    /// elapsed time and not on how many times it was called — a fixed step would
    /// make the pet trail further at low frame rates.
    /// </summary>
    public static class FollowLogic
    {
        /// <summary>
        /// B5: the point this weapon wants to be — <paramref name="standoff"/> metres
        /// from the target, on the side it is already on. Never inside the player.
        /// </summary>
        public static Float3 DesiredPosition(Float3 current, Float3 target, float standoff)
        {
            Float3 away = current - target;
            Float3 direction = away.Normalized();

            if (direction.Equals(Float3.Zero))
            {
                // Sitting exactly on the player carries no direction to back off along.
                direction = new Float3(0f, 0f, -1f);
            }

            return target + direction * standoff;
        }

        /// <summary>
        /// B4/B6: exponential approach. Covers the same fraction of the remaining gap
        /// per unit of time regardless of step count.
        /// </summary>
        public static Float3 Step(Float3 current, Float3 desired, float speed, float deltaTime)
        {
            if (speed <= 0f || deltaTime <= 0f)
            {
                return current;
            }

            float t = 1f - (float)Math.Exp(-speed * deltaTime);
            return current + (desired - current) * t;
        }
    }

    /// <summary>
    /// WPN-008a (B9): everything inside an orbit/follow weapon's reach.
    ///
    /// Unlike the tesla, which picks one target, a swinging weapon hits everyone it
    /// passes through — that is what makes it good when surrounded.
    /// </summary>
    public static class SweepLogic
    {
        /// <summary>
        /// Writes the index of every position within <paramref name="radius"/> into
        /// <paramref name="into"/> and returns how many were written. Only the first
        /// <paramref name="count"/> entries are read — a pooled list reuses its array.
        /// </summary>
        public static int AllInRange(Float3 origin, Float3[] positions, int count, float radius, int[] into)
        {
            if (positions == null || into == null || count <= 0 || radius <= 0f)
            {
                return 0;
            }

            if (count > positions.Length)
            {
                count = positions.Length;
            }

            float limit = radius * radius; // touching exactly at the radius counts
            int found = 0;

            for (int i = 0; i < count && found < into.Length; i++)
            {
                if ((positions[i] - origin).SqrMagnitude() <= limit)
                {
                    into[found++] = i;
                }
            }

            return found;
        }
    }
}
