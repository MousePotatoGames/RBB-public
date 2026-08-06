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

        [Tooltip("PAS-001: 최대 HP 보너스의 출처. 없으면 기준값 그대로 (설계 판단 1)")]
        [SerializeField] private PlayerProgress progress;

        [Tooltip("F05 임시: HUD가 없어 HP를 콘솔로 확인한다 (F14에서 HUD로 대체)")]
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

        public PlayerProgress Progress { get => progress; set => progress = value; }

        public float Current => _state.Current;

        /// <summary>
        /// PAS-001 (B21): base + passive bonus. The bonus is <b>added here</b> rather
        /// than written into <c>config.playerMaxHealth</c> — a ScriptableObject edit
        /// would outlive play mode and buff the next run (설계 판단 1).
        /// </summary>
        public float Max
        {
            get
            {
                float baseMax = config != null ? config.playerMaxHealth : 0f;
                return baseMax + (progress != null ? progress.Effects.MaxHealthBonus : 0f);
            }
        }

        public bool IsAlive => _state.IsAlive;
        public bool IsInvulnerable => _state.IsInvulnerable;

        private void Awake()
        {
            if (progress == null)
            {
                progress = GetComponent<PlayerProgress>();
            }

            if (!_initialised)
            {
                ResetHealth();
            }
        }

        /// <summary>PAS-001 (B21): restores health, clamped to the effective max.</summary>
        public void Heal(float amount)
        {
            _state = HealthLogic.Heal(_state, amount, Max);
            debugCurrentHealth = _state.Current;

            if (logToConsole)
            {
                Debug.Log($"[HP] +{amount:0.#} → {_state.Current:0.#}/{Max:0.#}", this);
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
