using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Single home for the F01/F03 TEMPORARY tuning values (MOVE-005).
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

        [Header("MOVE-002 / MOVE-003 — steering")]
        [Tooltip("Airborne acceleration multiplier (1 = same as ground)")]
        [Range(0f, 1f)] public float airControl = 0.35f;

        [Tooltip("Turn-component multiplier at max speed (1 = no damping)")]
        [Range(0.1f, 1f)] public float steeringAtMaxSpeed = 0.4f;

        [Header("JUMP-001 — TEMPORARY")]
        [Tooltip("Total air time of a jump, ground to ground (range 0.55~0.8)")]
        [Min(0.1f)] public float jumpAirTime = 0.65f;

        [Tooltip("Grace period after leaving ground (range 0.08~0.12)")]
        [Min(0f)] public float coyoteTime = 0.1f;

        [Tooltip("How long a jump press stays queued while airborne")]
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("DASH-001 — TEMPORARY")]
        [Tooltip("Planar speed added by one dash (range 7~10)")]
        [Min(0f)] public float dashSpeed = 8f;

        [Tooltip("Seconds before the dash is available again (range 1.5~2.2)")]
        [Min(0f)] public float dashCooldown = 1.8f;

        [Header("SPD-001 — speed tiers (TEMPORARY)")]
        [Range(0f, 1f)] public float midTierThreshold = 0.35f;
        [Range(0f, 1f)] public float highTierThreshold = 0.70f;
        [Range(0f, 1f)] public float rumbleTierThreshold = 0.95f;

        [Tooltip("How far below a boundary the ball must fall before dropping a tier")]
        [Range(0f, 0.2f)] public float tierHysteresis = 0.05f;

        [Tooltip("Seconds after a dash during which the ball counts as dashing (SPD-001 exception)")]
        [Min(0f)] public float dashActiveWindow = 0.35f;

        public float Acceleration => maxSpeed / Mathf.Max(0.05f, timeToMaxSpeed);

        /// <summary>Launch speed that produces jumpAirTime under the current gravity.</summary>
        public float JumpSpeed => JumpLogic.JumpSpeedForAirTime(jumpAirTime, Mathf.Abs(Physics.gravity.y));

        public MoveConfig ToMoveConfig() => new MoveConfig(maxSpeed, Acceleration, slopeAssist, airControl, steeringAtMaxSpeed);

        public JumpConfig ToJumpConfig() => new JumpConfig(coyoteTime, jumpBufferTime);

        public DashConfig ToDashConfig() => new DashConfig(dashSpeed, dashCooldown);

        public SpeedTierConfig ToSpeedTierConfig() =>
            new SpeedTierConfig(midTierThreshold, highTierThreshold, rumbleTierThreshold, tierHysteresis);
    }
}
