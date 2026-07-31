using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for F04 speed tiers (Docs/Features/F04-speed-tiers.md).
    /// Test names carry the GAME_RULES id (SPD-001).
    /// </summary>
    public sealed class SpeedTierLogicTests
    {
        private const float MaxSpeed = 12f;

        private static SpeedTierConfig Config =>
            new SpeedTierConfig(midThreshold: 0.35f, highThreshold: 0.70f, rumbleThreshold: 0.95f, hysteresis: 0.05f);

        private static SpeedTier Eval(float ratio, SpeedTier current = SpeedTier.Low, bool dashing = false)
        {
            return SpeedTierLogic.Evaluate(ratio * MaxSpeed, MaxSpeed, dashing, current, Config);
        }

        // B1 — SPD-001
        [Test]
        public void Spd001_Ratios_MapToExpectedTiers()
        {
            Assert.AreEqual(SpeedTier.Low, Eval(0f));
            Assert.AreEqual(SpeedTier.Mid, Eval(0.5f));
            Assert.AreEqual(SpeedTier.High, Eval(0.8f));
            Assert.AreEqual(SpeedTier.Rumble, Eval(1f));
        }

        // B1 — SPD-001
        [Test]
        public void Spd001_Boundaries_AreInclusiveAsSpecified()
        {
            Assert.AreEqual(SpeedTier.Mid, Eval(0.35f), "35% is the start of Mid");
            Assert.AreEqual(SpeedTier.Low, Eval(0.349f), "just below 35% is still Low");
            Assert.AreEqual(SpeedTier.High, Eval(0.70f), "70% is the start of High");
            Assert.AreEqual(SpeedTier.Rumble, Eval(0.95f), "95% is the start of Rumble");
        }

        // B2 — SPD-001 exception / DASH-001
        [Test]
        public void Spd001_Dashing_ForcesRumble_RegardlessOfSpeed()
        {
            Assert.AreEqual(SpeedTier.Rumble, Eval(0f, SpeedTier.Low, dashing: true),
                "SPD-001: a dash is Rumble even from a standstill");
            Assert.AreEqual(SpeedTier.Rumble, Eval(0.4f, SpeedTier.Mid, dashing: true));
        }

        // B3 — SPD-001 (no flicker)
        [Test]
        public void Spd001_Hysteresis_HoldsTierNearBoundary()
        {
            // Sitting just under the High boundary while already High must stay High.
            Assert.AreEqual(SpeedTier.High, Eval(0.68f, SpeedTier.High),
                "within the hysteresis band the tier must hold");
        }

        // B3 — SPD-001
        [Test]
        public void Spd001_Hysteresis_ReleasesTierBeyondBoundary()
        {
            // 0.60 is a full hysteresis band (0.05) below the 0.70 boundary.
            Assert.AreEqual(SpeedTier.Mid, Eval(0.60f, SpeedTier.High),
                "past the hysteresis band the tier must drop");
        }

        // B3 — SPD-001 (a large drop must not stop at one step)
        [Test]
        public void Spd001_LargeDrop_FallsMultipleTiers()
        {
            Assert.AreEqual(SpeedTier.Low, Eval(0.05f, SpeedTier.Rumble),
                "a hard stop must fall straight to Low");
        }

        // B5 — SPD-001
        [Test]
        public void Spd001_OverMaxSpeed_ClampsToRumble()
        {
            Assert.AreEqual(SpeedTier.Rumble, Eval(2.5f, SpeedTier.Rumble), "no tier above Rumble exists");
        }

        // 방어 — 0 나눗셈
        [Test]
        public void Spd001_ZeroMaxSpeed_DoesNotDivideByZero()
        {
            var tier = SpeedTierLogic.Evaluate(5f, 0f, false, SpeedTier.High, Config);

            Assert.AreEqual(SpeedTier.Low, tier, "a zero max speed must degrade safely");
        }
    }
}
