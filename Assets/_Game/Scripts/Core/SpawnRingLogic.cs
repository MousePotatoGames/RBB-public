using System;

namespace Game.Core
{
    /// <summary>
    /// WAVE-002: places spawns on a ring around the player, outside the camera's
    /// front cone, spreading successive angles so waves do not clump on one side.
    /// </summary>
    public static class SpawnRingLogic
    {
        private const float DegToRad = (float)(Math.PI / 180.0);

        /// <summary>Planar unit direction for a ring angle in degrees (0° = +Z).</summary>
        public static Float3 SpawnDirection(float angleDegrees)
        {
            float rad = angleDegrees * DegToRad;
            return new Float3((float)Math.Sin(rad), 0f, (float)Math.Cos(rad));
        }

        /// <summary>B6: true when the direction falls inside the camera's forward cone (must not spawn there).</summary>
        public static bool IsInsideCameraCone(Float3 direction, Float3 cameraForward, float halfAngleDegrees)
        {
            Float3 dir = new Float3(direction.X, 0f, direction.Z).Normalized();
            Float3 forward = new Float3(cameraForward.X, 0f, cameraForward.Z).Normalized();
            if (dir.Equals(Float3.Zero) || forward.Equals(Float3.Zero))
            {
                return false;
            }

            float dot = Float3.Dot(dir, forward);
            if (dot > 1f) dot = 1f;
            if (dot < -1f) dot = -1f;

            float angle = (float)(Math.Acos(dot) / DegToRad);
            return angle <= halfAngleDegrees;
        }

        /// <summary>B7: next ring angle, advanced by a step that does not repeat quickly (golden angle).</summary>
        public static float NextAngle(float previousAngle, float stepDegrees)
        {
            float next = previousAngle + stepDegrees;
            next %= 360f;
            if (next < 0f)
            {
                next += 360f;
            }

            return next;
        }

        /// <summary>
        /// B6/B7: first angle at or after <paramref name="startAngle"/> that sits
        /// outside the camera cone. Falls back to the opposite of camera forward
        /// if the search somehow exhausts (defensive; cone is far below 360°).
        /// </summary>
        public static float FirstAngleOutsideCone(float startAngle, float stepDegrees, Float3 cameraForward, float halfAngleDegrees, int maxAttempts = 16)
        {
            float angle = startAngle;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (!IsInsideCameraCone(SpawnDirection(angle), cameraForward, halfAngleDegrees))
                {
                    return angle;
                }

                angle = NextAngle(angle, stepDegrees);
            }

            Float3 behind = new Float3(-cameraForward.X, 0f, -cameraForward.Z).Normalized();
            return (float)(Math.Atan2(behind.X, behind.Z) / DegToRad);
        }

        /// <summary>Ring position for the player, at the given angle and radius.</summary>
        public static Float3 SpawnPosition(Float3 playerPosition, float angleDegrees, float radius)
        {
            return playerPosition + SpawnDirection(angleDegrees) * radius;
        }
    }
}
