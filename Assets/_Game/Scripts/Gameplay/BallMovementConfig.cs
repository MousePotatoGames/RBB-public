using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Single home for the F01 TEMPORARY tuning values (MOVE-005).
    /// Playtest tuning happens here, never as magic numbers in code.
    /// </summary>
    [CreateAssetMenu(menuName = "RumbleBall/Ball Movement Config", fileName = "BallMovementConfig")]
    public sealed class BallMovementConfig : ScriptableObject
    {
        [Header("MOVE-005 — TEMPORARY tuning (range 10~13)")]
        [Min(0.1f)] public float maxSpeed = 12f;

        [Tooltip("Seconds from standstill to max speed (range 1.0~1.5)")]
        [Min(0.05f)] public float timeToMaxSpeed = 1.2f;

        [Header("MOVE-004 — slope assist")]
        [Min(0f)] public float slopeAssist = 1f;

        [Tooltip("SphereCast distance below the ball center for ground detection")]
        [Min(0.1f)] public float groundCheckDistance = 0.6f;

        public float Acceleration => maxSpeed / Mathf.Max(0.05f, timeToMaxSpeed);

        public MoveConfig ToMoveConfig() => new MoveConfig(maxSpeed, Acceleration, slopeAssist);
    }
}
