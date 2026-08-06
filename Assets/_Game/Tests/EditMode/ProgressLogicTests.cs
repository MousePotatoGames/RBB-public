using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// XP-001 / XP-002 coverage (Docs/Features/F13-xp-levelup-passives.md).
    /// </summary>
    public sealed class ProgressLogicTests
    {
        /// <summary>XP-002's TEMPORARY curve, cumulative.</summary>
        private static readonly float[] Curve = { 15f, 35f, 65f, 105f };

        private static Float3 At(float x, float z) => new Float3(x, 0f, z);

        // ---- ExperienceLogic: the curve ------------------------------------------

        [Test]
        public void Xp002_LevelFor_ZeroExperience_IsLevelOne()
        {
            Assert.AreEqual(1, ExperienceLogic.LevelFor(0f, Curve));
        }

        [Test]
        public void Xp002_LevelFor_ReturnsOneBelowFirstThreshold()
        {
            Assert.AreEqual(1, ExperienceLogic.LevelFor(14.9f, Curve));
        }

        // The boundary is the rule: XP-002 says "요구량 도달", not "초과".
        [Test]
        public void Xp002_LevelFor_ExactThreshold_LevelsUp()
        {
            Assert.AreEqual(2, ExperienceLogic.LevelFor(15f, Curve));
        }

        [Test]
        public void Xp002_LevelFor_WalksTheWholeCurve()
        {
            Assert.AreEqual(2, ExperienceLogic.LevelFor(34f, Curve));
            Assert.AreEqual(3, ExperienceLogic.LevelFor(35f, Curve));
            Assert.AreEqual(4, ExperienceLogic.LevelFor(65f, Curve));
            Assert.AreEqual(5, ExperienceLogic.LevelFor(105f, Curve));
        }

        // B9 — the curve is finite and XP-002 does not extrapolate it.
        [Test]
        public void Xp002_LevelFor_BeyondLastThreshold_StaysAtMax()
        {
            Assert.AreEqual(5, ExperienceLogic.LevelFor(10_000f, Curve));
        }

        [Test]
        public void Xp002_LevelFor_NoCurve_StaysAtLevelOne()
        {
            Assert.AreEqual(1, ExperienceLogic.LevelFor(999f, null));
        }

        // ---- ExperienceLogic: level-up count (B8) --------------------------------

        [Test]
        public void Xp002_PendingLevelUps_SingleGain_ReturnsOne()
        {
            Assert.AreEqual(1, ExperienceLogic.PendingLevelUps(1, 2));
        }

        // One orb can cross two thresholds, and LVL-001 owes a card for each.
        [Test]
        public void Xp002_PendingLevelUps_BigGain_ReturnsTwo()
        {
            int before = ExperienceLogic.LevelFor(14f, Curve);
            int after = ExperienceLogic.LevelFor(40f, Curve);

            Assert.AreEqual(2, ExperienceLogic.PendingLevelUps(before, after),
                "XP-002 B8: 14 -> 40 crosses both 15 and 35");
        }

        [Test]
        public void Xp002_PendingLevelUps_NoChange_ReturnsZero()
        {
            Assert.AreEqual(0, ExperienceLogic.PendingLevelUps(3, 3));
        }

        // ---- ExperienceLogic: progress (F14's HUD reads this) --------------------

        [Test]
        public void Xp002_Progress_HalfwayThroughFirstLevel()
        {
            Assert.AreEqual(0.5f, ExperienceLogic.Progress(7.5f, Curve), 1e-3f);
        }

        [Test]
        public void Xp002_Progress_MeasuresFromTheLevelFloor_NotZero()
        {
            // Level 2 spans 15..35, so 25 is halfway — not 25/35.
            Assert.AreEqual(0.5f, ExperienceLogic.Progress(25f, Curve), 1e-3f);
        }

        [Test]
        public void Xp002_Progress_AtMaxLevel_IsFull()
        {
            Assert.AreEqual(1f, ExperienceLogic.Progress(200f, Curve), 1e-3f);
        }

        // ---- MagnetLogic (B3~B5) -------------------------------------------------

        [Test]
        public void Xp001_Magnet_InRange_IsTrueInside()
        {
            Assert.IsTrue(MagnetLogic.InRange(At(1f, 0f), At(0f, 0f), radius: 2.5f));
        }

        [Test]
        public void Xp001_Magnet_InRange_IsFalseOutside()
        {
            Assert.IsFalse(MagnetLogic.InRange(At(3f, 0f), At(0f, 0f), radius: 2.5f));
        }

        [Test]
        public void Xp001_Magnet_InRange_ExactRadius_IsIncluded()
        {
            Assert.IsTrue(MagnetLogic.InRange(At(2.5f, 0f), At(0f, 0f), radius: 2.5f),
                "XP-001 B3: the boundary is inside, matching every other radius test in the project");
        }

        [Test]
        public void Xp001_Magnet_Step_MovesTowardThePlayer()
        {
            Float3 next = MagnetLogic.Step(At(4f, 0f), At(0f, 0f), speed: 9f, deltaTime: 0.1f);

            Assert.AreEqual(3.1f, next.X, 1e-3f);
            Assert.AreEqual(0f, next.Z, 1e-3f);
        }

        // Overshoot would make the orb oscillate around the ball instead of being eaten.
        [Test]
        public void Xp001_Magnet_Step_DoesNotOvershoot()
        {
            Float3 next = MagnetLogic.Step(At(0.2f, 0f), At(0f, 0f), speed: 9f, deltaTime: 0.5f);

            Assert.AreEqual(0f, next.X, 1e-4f);
            Assert.AreEqual(0f, next.Z, 1e-4f);
        }

        [Test]
        public void Xp001_Magnet_Step_AlreadyOnPlayer_DoesNotProduceNaN()
        {
            Float3 next = MagnetLogic.Step(At(0f, 0f), At(0f, 0f), speed: 9f, deltaTime: 0.1f);

            Assert.IsFalse(float.IsNaN(next.X) || float.IsNaN(next.Z));
        }

        [Test]
        public void Xp001_Magnet_IsAbsorbed_OnlyWhenClose()
        {
            Assert.IsTrue(MagnetLogic.IsAbsorbed(At(0.5f, 0f), At(0f, 0f), absorbDistance: 0.6f));
            Assert.IsFalse(MagnetLogic.IsAbsorbed(At(1.2f, 0f), At(0f, 0f), absorbDistance: 0.6f));
        }
    }
}
