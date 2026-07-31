using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Enemy HP with the per-enemy hit cooldown (DMG-002) and single death
    /// handling (B13). Death itself is reported; the corpse timing and pool
    /// return live in ScrapDrone / DroneSpawner.
    /// </summary>
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private DroneConfig config;

        private float _current;
        private float _lastHitTime = HitCooldownLogic.NeverHit;

        /// <summary>Raised once when HP reaches zero.</summary>
        public event Action<EnemyHealth> Died;

        public DroneConfig Config { get => config; set => config = value; }
        public float Current => _current;
        public bool IsAlive { get; private set; } = true;

        /// <summary>Resets HP and cooldown for pool reuse (B14).</summary>
        public void ResetHealth()
        {
            _current = config != null ? config.droneMaxHealth : 1f;
            _lastHitTime = HitCooldownLogic.NeverHit;
            IsAlive = true;
        }

        private void Awake()
        {
            if (_current <= 0f)
            {
                ResetHealth();
            }
        }

        /// <summary>B6/B7: true when this enemy may be damaged again.</summary>
        public bool CanBeHit(float now)
        {
            if (!IsAlive || config == null)
            {
                return false;
            }

            return HitCooldownLogic.CanHit(_lastHitTime, now, config.hitCooldown);
        }

        /// <summary>
        /// B12/B13: applies damage if alive and off cooldown. Returns true when the
        /// hit landed, so the caller knows whether to knock back and freeze.
        /// </summary>
        public bool TryTakeDamage(float amount, float now)
        {
            if (!CanBeHit(now) || amount <= 0f)
            {
                return false;
            }

            _lastHitTime = now;
            _current -= amount;

            if (_current > 0f)
            {
                return true;
            }

            _current = 0f;
            if (IsAlive)
            {
                IsAlive = false;
                Died?.Invoke(this);
            }

            return true;
        }
    }
}
