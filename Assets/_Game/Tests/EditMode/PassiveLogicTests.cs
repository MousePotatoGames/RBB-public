using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// PAS-004 coverage (Docs/Features/F13-xp-levelup-passives.md).
    /// Passive <i>effects</i> land in stage 2 and get their own tests there.
    /// </summary>
    public sealed class PassiveLogicTests
    {
        private static readonly PassiveKind[] Buffer = new PassiveKind[8];

        [Test]
        public void Pas004_Empty_HasNoStages()
        {
            var state = PassiveState.Empty;

            Assert.AreEqual(0, state.StageOf(PassiveKind.ArmorPlating));
            Assert.AreEqual(0, state.StageOf(PassiveKind.DenseCore));
            Assert.AreEqual(0, state.StageOf(PassiveKind.MagnetField));
            Assert.AreEqual(0, state.TotalStages);
        }

        // default(PassiveState) has a null array inside — it must read as empty, not throw.
        [Test]
        public void Pas004_DefaultState_BehavesAsEmpty()
        {
            PassiveState state = default;

            Assert.AreEqual(0, state.StageOf(PassiveKind.MagnetField));
            Assert.AreEqual(PassiveState.Empty, state);
        }

        [Test]
        public void Pas004_Upgrade_RaisesStageByOne()
        {
            PassiveState state = PassiveLogic.Upgrade(PassiveState.Empty, PassiveKind.DenseCore);

            Assert.AreEqual(1, state.StageOf(PassiveKind.DenseCore));
        }

        [Test]
        public void Pas004_Upgrade_LeavesOtherPassivesAlone()
        {
            PassiveState state = PassiveLogic.Upgrade(PassiveState.Empty, PassiveKind.DenseCore);

            Assert.AreEqual(0, state.StageOf(PassiveKind.ArmorPlating));
            Assert.AreEqual(0, state.StageOf(PassiveKind.MagnetField));
        }

        [Test]
        public void Pas004_Upgrade_DoesNotMutateTheOriginal()
        {
            PassiveState before = PassiveLogic.Upgrade(PassiveState.Empty, PassiveKind.ArmorPlating);
            PassiveLogic.Upgrade(before, PassiveKind.ArmorPlating);

            Assert.AreEqual(1, before.StageOf(PassiveKind.ArmorPlating),
                "PassiveState is a value: upgrading a copy must not reach back into it");
        }

        // PAS-004: 상한 3단계.
        [Test]
        public void Pas004_Upgrade_StopsAtThree()
        {
            PassiveState state = PassiveState.Empty;
            for (int i = 0; i < 10; i++)
            {
                state = PassiveLogic.Upgrade(state, PassiveKind.MagnetField);
            }

            Assert.AreEqual(PassiveLogic.MaxStage, state.StageOf(PassiveKind.MagnetField));
        }

        [Test]
        public void Pas004_TotalStages_CountsEveryPassive()
        {
            PassiveState state = PassiveLogic.Upgrade(PassiveState.Empty, PassiveKind.ArmorPlating);
            state = PassiveLogic.Upgrade(state, PassiveKind.ArmorPlating);
            state = PassiveLogic.Upgrade(state, PassiveKind.MagnetField);

            Assert.AreEqual(3, state.TotalStages);
        }

        // ---- card candidates (B19/B20) -------------------------------------------

        [Test]
        public void Pas004_Candidates_EmptyState_OffersEveryPassive()
        {
            int count = PassiveLogic.Candidates(PassiveState.Empty, Buffer);

            Assert.AreEqual(PassiveLogic.KindCount, count,
                "three passives against three card slots — nothing to draw, everything is offered");
        }

        [Test]
        public void Pas004_Candidates_ExcludesMaxedPassive()
        {
            PassiveState state = PassiveState.Empty;
            for (int i = 0; i < PassiveLogic.MaxStage; i++)
            {
                state = PassiveLogic.Upgrade(state, PassiveKind.DenseCore);
            }

            int count = PassiveLogic.Candidates(state, Buffer);

            Assert.AreEqual(PassiveLogic.KindCount - 1, count);
            for (int i = 0; i < count; i++)
            {
                Assert.AreNotEqual(PassiveKind.DenseCore, Buffer[i], "PAS-004 Exception: stage 3 leaves the pool");
            }
        }

        [Test]
        public void Pas004_Candidates_PartiallyLevelled_StillOffered()
        {
            PassiveState state = PassiveLogic.Upgrade(PassiveState.Empty, PassiveKind.DenseCore);

            Assert.AreEqual(PassiveLogic.KindCount, PassiveLogic.Candidates(state, Buffer));
        }

        // B20 — the caller must not stop time for an empty screen.
        [Test]
        public void Pas004_Candidates_AllMaxed_ReturnsEmpty()
        {
            PassiveState state = PassiveState.Empty;
            for (int k = 0; k < PassiveLogic.KindCount; k++)
            {
                for (int i = 0; i < PassiveLogic.MaxStage; i++)
                {
                    state = PassiveLogic.Upgrade(state, (PassiveKind)k);
                }
            }

            Assert.AreEqual(0, PassiveLogic.Candidates(state, Buffer));
        }

        [Test]
        public void Pas004_Candidates_ClampsToBuffer()
        {
            var tiny = new PassiveKind[1];

            Assert.AreEqual(1, PassiveLogic.Candidates(PassiveState.Empty, tiny),
                "a short buffer must truncate, not overrun");
        }

        [Test]
        public void Pas004_Candidates_NullBuffer_ReturnsZero()
        {
            Assert.AreEqual(0, PassiveLogic.Candidates(PassiveState.Empty, null));
        }

        // ---- effects (B21~B23) ----------------------------------------------------

        /// <summary>The shipped tuning: armour adds, the other two multiply.</summary>
        private static PassiveEffect[] Tuning() => new[]
        {
            new PassiveEffect(maxHealth: 20f, heal: 20f, speed: 1f, damage: 1f, magnet: 1f, experience: 1f),
            new PassiveEffect(0f, 0f, speed: 1.08f, damage: 1.15f, magnet: 1f, experience: 1f),
            new PassiveEffect(0f, 0f, 1f, 1f, magnet: 1.6f, experience: 1.25f),
        };

        private static PassiveState Staged(PassiveKind kind, int stages)
        {
            PassiveState state = PassiveState.Empty;
            for (int i = 0; i < stages; i++)
            {
                state = PassiveLogic.Upgrade(state, kind);
            }

            return state;
        }

        // The guarantee behind 설계 판단 1: with nothing taken, every consumer reads
        // its own base value back untouched.
        [Test]
        public void Pas004_NoStages_EveryEffectIsNeutral()
        {
            PassiveEffects e = PassiveLogic.Accumulate(PassiveState.Empty, Tuning());

            Assert.AreEqual(0f, e.MaxHealthBonus);
            Assert.AreEqual(1f, e.SpeedMultiplier);
            Assert.AreEqual(1f, e.DamageMultiplier);
            Assert.AreEqual(1f, e.MagnetMultiplier);
            Assert.AreEqual(1f, e.ExperienceMultiplier);
        }

        [Test]
        public void Pas001_MaxHealth_AddsPerStage()
        {
            Assert.AreEqual(20f, PassiveLogic.Accumulate(Staged(PassiveKind.ArmorPlating, 1), Tuning()).MaxHealthBonus, 1e-3f);
            Assert.AreEqual(60f, PassiveLogic.Accumulate(Staged(PassiveKind.ArmorPlating, 3), Tuning()).MaxHealthBonus, 1e-3f);
        }

        [Test]
        public void Pas001_HealOnTake_IsPerStage_NotCumulative()
        {
            Assert.AreEqual(20f, PassiveLogic.HealOnTake(PassiveKind.ArmorPlating, Tuning()), 1e-3f);
            Assert.AreEqual(0f, PassiveLogic.HealOnTake(PassiveKind.DenseCore, Tuning()), 1e-3f);
        }

        // Multipliers compound: 1.08³, not 1 + 0.08 × 3.
        [Test]
        public void Pas002_SpeedMultiplier_CompoundsPerStage()
        {
            PassiveEffects e = PassiveLogic.Accumulate(Staged(PassiveKind.DenseCore, 3), Tuning());

            Assert.AreEqual(1.08f * 1.08f * 1.08f, e.SpeedMultiplier, 1e-4f);
        }

        [Test]
        public void Pas002_DamageMultiplier_CompoundsPerStage()
        {
            PassiveEffects e = PassiveLogic.Accumulate(Staged(PassiveKind.DenseCore, 2), Tuning());

            Assert.AreEqual(1.15f * 1.15f, e.DamageMultiplier, 1e-4f);
        }

        [Test]
        public void Pas003_MagnetAndExperience_CompoundTogether()
        {
            PassiveEffects e = PassiveLogic.Accumulate(Staged(PassiveKind.MagnetField, 3), Tuning());

            Assert.AreEqual(1.6f * 1.6f * 1.6f, e.MagnetMultiplier, 1e-3f);
            Assert.AreEqual(1.25f * 1.25f * 1.25f, e.ExperienceMultiplier, 1e-4f);
        }

        // Each passive touches its own fields only — a mixed build must not blur them.
        [Test]
        public void Pas004_MixedStages_StayIndependent()
        {
            PassiveState state = PassiveLogic.Upgrade(PassiveState.Empty, PassiveKind.ArmorPlating);
            state = PassiveLogic.Upgrade(state, PassiveKind.MagnetField);

            PassiveEffects e = PassiveLogic.Accumulate(state, Tuning());

            Assert.AreEqual(20f, e.MaxHealthBonus, 1e-3f);
            Assert.AreEqual(1f, e.SpeedMultiplier, 1e-4f, "고밀도 코어 was never taken");
            Assert.AreEqual(1.6f, e.MagnetMultiplier, 1e-3f);
        }

        [Test]
        public void Pas004_Accumulate_NullTuning_IsNeutral()
        {
            PassiveEffects e = PassiveLogic.Accumulate(Staged(PassiveKind.DenseCore, 3), null);

            Assert.AreEqual(1f, e.SpeedMultiplier);
            Assert.AreEqual(0f, e.MaxHealthBonus);
        }
    }
}
