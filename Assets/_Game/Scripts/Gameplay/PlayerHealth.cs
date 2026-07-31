using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// HP-001/HP-002 adapter. Owns the health state and broadcasts
    /// PlayerDamaged / PlayerDied (기획서 14.3). Defeat handling itself is F07 —
    /// this component only reports the transition.
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private DroneConfig config;

        [Tooltip("F05 임시: HUD가 없어 HP를 콘솔로 확인한다 (F13에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole = true;

        [Header("Read-only (인스펙터 확인용)")]
        [SerializeField] private float debugCurrentHealth;

        private HealthState _state;
        private bool _initialised;

        /// <summary>PlayerDamaged(remaining, amount).</summary>
        public event Action<float, float> Damaged;

        /// <summary>PlayerDied — fired once (B16).</summary>
        public event Action Died;

        public DroneConfig Config
        {
            get => config;
            set
            {
                config = value;
                ResetHealth();
            }
        }

        public float Current => _state.Current;
        public float Max => config != null ? config.playerMaxHealth : 0f;
        public bool IsAlive => _state.IsAlive;
        public bool IsInvulnerable => _state.IsInvulnerable;

        private void Awake()
        {
            if (!_initialised)
            {
                ResetHealth();
            }
        }

        public void ResetHealth()
        {
            if (config == null)
            {
                return;
            }

            _state = HealthState.Full(config.ToHealthConfig());
            _initialised = true;
            debugCurrentHealth = _state.Current;
        }

        private void FixedUpdate()
        {
            _state = HealthLogic.Tick(_state, Time.fixedDeltaTime);
        }

        /// <summary>B13~B16: applies contact damage, honouring the invulnerability window.</summary>
        public void TakeDamage(float amount)
        {
            if (config == null)
            {
                return;
            }

            DamageResult result = HealthLogic.ApplyDamage(_state, amount, config.ToHealthConfig());
            _state = result.State;

            if (!result.Applied)
            {
                return;
            }

            debugCurrentHealth = _state.Current;
            Damaged?.Invoke(_state.Current, amount);

            if (logToConsole)
            {
                Debug.Log($"[HP] -{amount:0.#} → {_state.Current:0.#}/{Max:0.#}", this);
            }

            if (result.JustDied)
            {
                Died?.Invoke();
                if (logToConsole)
                {
                    Debug.Log("[HP] PlayerDied (F05에서는 게임이 계속됩니다 — 패배 처리는 F07)", this);
                }
            }
        }
    }
}
