using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// PAS-001~003 as data. One asset per passive.
    ///
    /// The numbers live here even though stage 1 applies none of them: the card has
    /// to <b>show</b> the effect, and formatting a number beats keeping a display
    /// string next to a value that can drift away from it. Stage 2 wires the same
    /// fields to their consumers.
    ///
    /// Effects are per stage. Additive fields (HP) sum; multiplier fields compound
    /// — three stages of 자기장 증폭 is ×1.6³, not ×1.6.
    /// </summary>
    [CreateAssetMenu(menuName = "RumbleBall/Passive Definition", fileName = "Passive_")]
    public sealed class PassiveDefinition : ScriptableObject
    {
        public PassiveKind kind = PassiveKind.ArmorPlating;

        [Tooltip("카드에 표시되는 이름. ASCII — 기본 TMP 폰트에 한글 글리프가 없다 (설계 판단 6)")]
        public string displayName = "ARMOR PLATING";

        [Header("PAS-001 — 강화 외피 (단계당 가산)")]
        public float maxHealthPerStage;
        public float healPerStage;

        [Header("PAS-002 — 고밀도 코어 (단계당 곱)")]
        [Min(1f)] public float speedMultiplierPerStage = 1f;
        [Min(1f)] public float damageMultiplierPerStage = 1f;

        [Header("PAS-003 — 자기장 증폭 (단계당 곱)")]
        [Min(1f)] public float magnetMultiplierPerStage = 1f;
        [Min(1f)] public float experienceMultiplierPerStage = 1f;

        /// <summary>
        /// The card body: one line per effect this passive actually has. Only
        /// non-neutral fields are printed, so a passive never shows "SPEED x1.00".
        /// ASCII, padded to line up in a monospace-ish column.
        /// </summary>
        public string EffectLines()
        {
            string text = string.Empty;
            text = Append(text, maxHealthPerStage != 0f, $"MAX HP  +{maxHealthPerStage:0.#}");
            text = Append(text, healPerStage != 0f, $"HEAL     {healPerStage:0.#}");
            text = Append(text, speedMultiplierPerStage != 1f, $"SPEED   x{speedMultiplierPerStage:0.00}");
            text = Append(text, damageMultiplierPerStage != 1f, $"DAMAGE  x{damageMultiplierPerStage:0.00}");
            text = Append(text, magnetMultiplierPerStage != 1f, $"RANGE   x{magnetMultiplierPerStage:0.00}");
            text = Append(text, experienceMultiplierPerStage != 1f, $"XP      x{experienceMultiplierPerStage:0.00}");
            return text;
        }

        private static string Append(string text, bool include, string line)
        {
            if (!include)
            {
                return text;
            }

            return text.Length == 0 ? line : text + "\n" + line;
        }

        /// <summary>
        /// Stage as pips — `[|..]` reads at a glance where a digit does not, and
        /// LVL-001 gives the player 0.2 seconds.
        /// </summary>
        public static string StagePips(int stage, int maxStage)
        {
            var chars = new char[maxStage + 2];
            chars[0] = '[';
            for (int i = 0; i < maxStage; i++)
            {
                chars[i + 1] = i < stage ? '|' : '.';
            }

            chars[maxStage + 1] = ']';
            return new string(chars);
        }
    }
}
