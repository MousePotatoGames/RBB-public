using System;

namespace Game.Core
{
    /// <summary>PAS-001~003. Order is the card order on screen, left to right.</summary>
    public enum PassiveKind
    {
        ArmorPlating = 0, // PAS-001 강화 외피
        DenseCore = 1,    // PAS-002 고밀도 코어
        MagnetField = 2,  // PAS-003 자기장 증폭
    }

    /// <summary>
    /// PAS-004 (B17): which stage each passive is at, 0 = not taken.
    ///
    /// The array is sized from the enum rather than a literal — WeaponSlots shipped
    /// a <c>new bool[3]</c> that broke silently the moment a fourth weapon kind
    /// existed, and this is the same shape of state.
    /// </summary>
    public readonly struct PassiveState : IEquatable<PassiveState>
    {
        private static readonly int KindCount = Enum.GetValues(typeof(PassiveKind)).Length;

        private readonly int[] _stages;

        private PassiveState(int[] stages)
        {
            _stages = stages;
        }

        /// <summary>Nothing taken. <c>default(PassiveState)</c> means the same thing.</summary>
        public static PassiveState Empty => default;

        public int StageOf(PassiveKind kind)
        {
            int index = (int)kind;
            return _stages == null || index < 0 || index >= _stages.Length ? 0 : _stages[index];
        }

        /// <summary>Total stages taken across all passives — one per level-up.</summary>
        public int TotalStages
        {
            get
            {
                if (_stages == null)
                {
                    return 0;
                }

                int sum = 0;
                for (int i = 0; i < _stages.Length; i++)
                {
                    sum += _stages[i];
                }

                return sum;
            }
        }

        /// <summary>Returns a new state with one passive raised. Never mutates this one.</summary>
        internal PassiveState WithStage(PassiveKind kind, int stage)
        {
            var next = new int[KindCount];
            if (_stages != null)
            {
                Array.Copy(_stages, next, Math.Min(_stages.Length, next.Length));
            }

            int index = (int)kind;
            if (index >= 0 && index < next.Length)
            {
                next[index] = stage;
            }

            return new PassiveState(next);
        }

        public bool Equals(PassiveState other)
        {
            for (int i = 0; i < KindCount; i++)
            {
                if (StageOf((PassiveKind)i) != other.StageOf((PassiveKind)i))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj) => obj is PassiveState other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < KindCount; i++)
            {
                hash = hash * 31 + StageOf((PassiveKind)i);
            }

            return hash;
        }
    }

    /// <summary>
    /// One passive's numbers, <b>per stage</b>. Additive fields sum across stages,
    /// multiplier fields compound — three stages of 자기장 증폭 is ×1.6³.
    /// </summary>
    public readonly struct PassiveEffect
    {
        public readonly float MaxHealth;
        public readonly float Heal;
        public readonly float Speed;
        public readonly float Damage;
        public readonly float Magnet;
        public readonly float Experience;

        public PassiveEffect(float maxHealth, float heal, float speed, float damage, float magnet, float experience)
        {
            MaxHealth = maxHealth;
            Heal = heal;
            Speed = speed;
            Damage = damage;
            Magnet = magnet;
            Experience = experience;
        }

        /// <summary>No effect — additive zero, multiplicative one.</summary>
        public static PassiveEffect None => new PassiveEffect(0f, 0f, 1f, 1f, 1f, 1f);
    }

    /// <summary>
    /// What the passives currently add up to. Consumers read this instead of
    /// writing to their config, which is what keeps a shared ScriptableObject from
    /// carrying a buff into the next run (설계 판단 1).
    /// </summary>
    public readonly struct PassiveEffects
    {
        public readonly float MaxHealthBonus;
        public readonly float SpeedMultiplier;
        public readonly float DamageMultiplier;
        public readonly float MagnetMultiplier;
        public readonly float ExperienceMultiplier;

        public PassiveEffects(float maxHealthBonus, float speed, float damage, float magnet, float experience)
        {
            MaxHealthBonus = maxHealthBonus;
            SpeedMultiplier = speed;
            DamageMultiplier = damage;
            MagnetMultiplier = magnet;
            ExperienceMultiplier = experience;
        }

        /// <summary>Nothing taken: every consumer reads its own base value back.</summary>
        public static PassiveEffects Neutral => new PassiveEffects(0f, 1f, 1f, 1f, 1f);
    }

    /// <summary>
    /// PAS-004 (B17, B19, B20): stage accumulation and which cards may be offered.
    ///
    /// There is deliberately no draw here. Three passives against three card slots
    /// leaves nothing to shuffle — the player's choice is "which of my three axes
    /// do I grow", not "which of a random three". What LVL-001 actually needs is a
    /// <b>filter</b>: drop the ones already at the cap.
    /// </summary>
    public static class PassiveLogic
    {
        /// <summary>PAS-004: the cap. Stage 3 passives leave the card pool.</summary>
        public const int MaxStage = 3;

        public static int KindCount => Enum.GetValues(typeof(PassiveKind)).Length;

        /// <summary>B17: raises one passive by a stage, clamped at the cap.</summary>
        public static PassiveState Upgrade(PassiveState state, PassiveKind kind, int maxStage = MaxStage)
        {
            int current = state.StageOf(kind);
            if (current >= maxStage)
            {
                return state;
            }

            return state.WithStage(kind, current + 1);
        }

        public static bool IsMaxed(PassiveState state, PassiveKind kind, int maxStage = MaxStage) =>
            state.StageOf(kind) >= maxStage;

        /// <summary>
        /// B19/B20: writes the offerable passives into <paramref name="into"/> and
        /// returns how many. Zero means every passive is capped, and LVL-001 has
        /// nothing to show — the caller must not stop time for an empty screen.
        /// </summary>
        public static int Candidates(PassiveState state, PassiveKind[] into, int maxStage = MaxStage)
        {
            if (into == null)
            {
                return 0;
            }

            int count = 0;
            int kinds = KindCount;

            for (int i = 0; i < kinds && count < into.Length; i++)
            {
                var kind = (PassiveKind)i;
                if (IsMaxed(state, kind, maxStage))
                {
                    continue;
                }

                into[count] = kind;
                count++;
            }

            return count;
        }

        /// <summary>
        /// B21~B23: what the taken stages add up to. <paramref name="perKind"/> is
        /// indexed by <see cref="PassiveKind"/>; a short array simply contributes
        /// nothing for the kinds it does not cover.
        /// </summary>
        public static PassiveEffects Accumulate(PassiveState state, PassiveEffect[] perKind)
        {
            float maxHealth = 0f;
            float speed = 1f;
            float damage = 1f;
            float magnet = 1f;
            float experience = 1f;

            if (perKind != null)
            {
                int kinds = KindCount;
                for (int i = 0; i < kinds && i < perKind.Length; i++)
                {
                    int stage = state.StageOf((PassiveKind)i);
                    if (stage <= 0)
                    {
                        continue;
                    }

                    PassiveEffect e = perKind[i];
                    maxHealth += e.MaxHealth * stage;
                    speed *= Pow(e.Speed, stage);
                    damage *= Pow(e.Damage, stage);
                    magnet *= Pow(e.Magnet, stage);
                    experience *= Pow(e.Experience, stage);
                }
            }

            return new PassiveEffects(maxHealth, speed, damage, magnet, experience);
        }

        /// <summary>
        /// How much a single new stage heals (PAS-001). Read at the moment the card
        /// is taken — it is a one-off, not part of the standing effects.
        /// </summary>
        public static float HealOnTake(PassiveKind kind, PassiveEffect[] perKind)
        {
            int index = (int)kind;
            return perKind == null || index < 0 || index >= perKind.Length ? 0f : perKind[index].Heal;
        }

        /// <summary>Integer power — stages are 0..3, so a loop beats Math.Pow's rounding.</summary>
        private static float Pow(float value, int exponent)
        {
            float result = 1f;
            for (int i = 0; i < exponent; i++)
            {
                result *= value;
            }

            return result;
        }
    }
}
