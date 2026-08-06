using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// XP-002 / PAS-004 (B7~B9, B13, B17): the single owner of experience, level and
    /// passive stages.
    ///
    /// Nothing here writes to a ScriptableObject. That is the whole point — every
    /// player tunable lives in a shared asset, and a passive that raised
    /// <c>config.playerMaxHealth</c> would survive play mode and show up as a config
    /// change nobody made. Stages live in this component, and consumers ask
    /// <see cref="PassiveLogic"/> for the effective value (설계 판단 1).
    /// </summary>
    public sealed class PlayerProgress : MonoBehaviour
    {
        [SerializeField] private ProgressConfig config;
        [SerializeField] private ExperienceOrbPool orbs;

        [Tooltip("PAS-001~003 에셋. 여기 있는 수치가 실효값 계산의 입력이다 (설계 판단 1)")]
        [SerializeField] private List<PassiveDefinition> passives = new List<PassiveDefinition>();

        [Tooltip("PAS-001: 단계를 얻는 즉시 회복시킬 대상")]
        [SerializeField] private PlayerHealth health;

        [Tooltip("F13 임시: HUD가 없어 레벨을 콘솔로 확인한다 (F14에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole = true;

        [Header("Read-only (인스펙터 확인용)")]
        [SerializeField] private float debugExperience;
        [SerializeField] private int debugLevel = ExperienceLogic.FirstLevel;

        private PassiveState _stages = PassiveState.Empty;
        private PassiveEffect[] _perKind;

        /// <summary>PlayerLevelUp(newLevel) — fires once per level (B13).</summary>
        public event Action<int> LevelUp;

        /// <summary>ExperienceCollected(total) — XP-001's event, after the gain.</summary>
        public event Action<float> ExperienceCollected;

        public ProgressConfig Config { get => config; set => config = value; }
        public ExperienceOrbPool Orbs { get => orbs; set => orbs = value; }
        public List<PassiveDefinition> PassiveAssets { get => passives; set { passives = value; _perKind = null; Recalculate(); } }
        public PlayerHealth Health { get => health; set => health = value; }

        public float TotalExperience { get; private set; }
        public int Level { get; private set; } = ExperienceLogic.FirstLevel;
        public PassiveState Passives => _stages;

        /// <summary>
        /// B18~B23: what every consumer reads instead of touching its config.
        /// Recomputed on each stage rather than per frame — passives change four
        /// times a run at most.
        /// </summary>
        public PassiveEffects Effects { get; private set; } = PassiveEffects.Neutral;

        public int StageOf(PassiveKind kind) => _stages.StageOf(kind);

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<PlayerHealth>();
            }

            Recalculate();
        }

        private void OnEnable()
        {
            if (orbs != null)
            {
                orbs.Collected += OnOrbCollected;
            }
        }

        private void OnDisable()
        {
            if (orbs != null)
            {
                orbs.Collected -= OnOrbCollected;
            }
        }

        private void OnOrbCollected(float value) => AddExperience(value);

        /// <summary>
        /// B7/B8: adds experience and raises one <see cref="LevelUp"/> per level
        /// crossed. A single orb can cross two thresholds and LVL-001 owes a card
        /// for each, so this loops rather than firing once.
        /// </summary>
        public void AddExperience(float amount)
        {
            if (amount <= 0f || config == null)
            {
                return;
            }

            TotalExperience += amount * Effects.ExperienceMultiplier; // B23
            debugExperience = TotalExperience;

            ExperienceCollected?.Invoke(TotalExperience);

            int levelAfter = ExperienceLogic.LevelFor(TotalExperience, config.levelThresholds);
            int pending = ExperienceLogic.PendingLevelUps(Level, levelAfter);

            for (int i = 0; i < pending; i++)
            {
                Level++;
                debugLevel = Level;

                if (logToConsole)
                {
                    Debug.Log($"[XP] Lv{Level} — 누적 {TotalExperience:0.#}", this);
                }

                LevelUp?.Invoke(Level);
            }
        }

        /// <summary>B17/B21: takes one stage of a passive. Capped by PAS-004.</summary>
        public void TakePassive(PassiveKind kind)
        {
            PassiveState before = _stages;
            _stages = PassiveLogic.Upgrade(_stages, kind);

            if (_stages.StageOf(kind) == before.StageOf(kind))
            {
                return; // already at the cap — no effect, and nothing to heal
            }

            Recalculate();

            // B21: the max went up first, so the heal can actually use the new room.
            float heal = PassiveLogic.HealOnTake(kind, _perKind);
            if (heal > 0f && health != null)
            {
                health.Heal(heal);
            }

            if (logToConsole)
            {
                Debug.Log($"[PAS] {kind} → {_stages.StageOf(kind)}/{PassiveLogic.MaxStage} " +
                          $"(hp+{Effects.MaxHealthBonus:0.#} spd×{Effects.SpeedMultiplier:0.00} " +
                          $"dmg×{Effects.DamageMultiplier:0.00} mag×{Effects.MagnetMultiplier:0.00} " +
                          $"xp×{Effects.ExperienceMultiplier:0.00})", this);
            }
        }

        private void Recalculate()
        {
            EnsurePerKind();
            Effects = PassiveLogic.Accumulate(_stages, _perKind);
        }

        /// <summary>
        /// Flattens the definition assets into a kind-indexed array once. Built here
        /// rather than read per query so no consumer ends up walking a list every frame
        /// (기획서 15장).
        /// </summary>
        private void EnsurePerKind()
        {
            if (_perKind != null)
            {
                return;
            }

            _perKind = new PassiveEffect[PassiveLogic.KindCount];
            for (int i = 0; i < _perKind.Length; i++)
            {
                _perKind[i] = PassiveEffect.None;
            }

            if (passives == null)
            {
                return;
            }

            for (int i = 0; i < passives.Count; i++)
            {
                PassiveDefinition d = passives[i];
                if (d == null)
                {
                    continue;
                }

                int index = (int)d.kind;
                if (index < 0 || index >= _perKind.Length)
                {
                    continue;
                }

                _perKind[index] = new PassiveEffect(
                    d.maxHealthPerStage,
                    d.healPerStage,
                    d.speedMultiplierPerStage,
                    d.damageMultiplierPerStage,
                    d.magnetMultiplierPerStage,
                    d.experienceMultiplierPerStage);
            }
        }
    }
}
