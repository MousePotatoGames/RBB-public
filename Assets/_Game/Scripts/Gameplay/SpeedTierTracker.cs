using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// SPD-001 adapter: evaluates the speed tier every physics step and raises
    /// PlayerSpeedTierChanged only on an actual change (B4). Presentation
    /// subscribes to this rather than polling speed itself.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BallMotor))]
    public sealed class SpeedTierTracker : MonoBehaviour
    {
        [SerializeField] private BallMovementConfig config;

        private Rigidbody _body;
        private BallMotor _motor;

        /// <summary>PlayerSpeedTierChanged (기획서 14.3).</summary>
        public event Action<SpeedTier> TierChanged;

        public SpeedTier CurrentTier { get; private set; } = SpeedTier.Low;

        /// <summary>How many times the tier actually changed. For tests and debugging.</summary>
        public int ChangeCount { get; private set; }

        public BallMovementConfig Config
        {
            get => config;
            set => config = value;
        }

        /// <summary>Current planar speed as a fraction of max speed.</summary>
        public float SpeedRatio
        {
            get
            {
                if (config == null || config.maxSpeed <= 0f)
                {
                    return 0f;
                }

                return PlanarSpeed / config.maxSpeed;
            }
        }

        private float PlanarSpeed
        {
            get
            {
                Vector3 v = _body.linearVelocity;
                v.y = 0f;
                return v.magnitude;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _motor = GetComponent<BallMotor>();
            if (config == null)
            {
                config = _motor.Config;
            }
        }

        private void OnEnable()
        {
            // Announce the starting tier so visuals do not begin out of sync.
            TierChanged?.Invoke(CurrentTier);
        }

        private void FixedUpdate()
        {
            if (config == null)
            {
                return;
            }

            SpeedTier next = SpeedTierLogic.Evaluate(
                PlanarSpeed,
                config.maxSpeed,
                _motor.IsDashActive,
                CurrentTier,
                config.ToSpeedTierConfig());

            if (next == CurrentTier)
            {
                return;
            }

            CurrentTier = next;
            ChangeCount++;
            TierChanged?.Invoke(next);
        }
    }
}
