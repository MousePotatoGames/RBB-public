using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// SPK-001 / WPN-008 coverage (Docs/Features/F09-crusher-spike.md).
    /// </summary>
    public sealed class ContactWeaponLogicTests
    {
        private static ContactWeaponConfig Config(float halfAngle = 60f) =>
            new ContactWeaponConfig(
                arcHalfAngleDegrees: halfAngle,
                bonusDamage: 8f,
                dashDamageMultiplier: 1.5f,
                dashKnockbackMultiplier: 1.4f,
                minSpeedMultiplier: 0.25f);

        private static readonly Float3 Up = new Float3(0f, 1f, 0f);

        // B3
        [Test]
        public void Spk001_EnemyInsideArc_AddsDamage()
        {
            float bonus = ContactWeaponLogic.BonusDamage(
                Up, Up, speed: 12f, maxSpeed: 12f, frontality: 1f, isDashing: false, Config());

            Assert.Greater(bonus, 0f, "SPK-001: a hit straight along the weapon must add damage");
        }

        // B4 — the point of the feature
        [Test]
        public void Spk001_EnemyOutsideArc_AddsNothing()
        {
            var behind = new Float3(0f, -1f, 0f);

            float bonus = ContactWeaponLogic.BonusDamage(
                Up, behind, speed: 12f, maxSpeed: 12f, frontality: 1f, isDashing: false, Config());

            Assert.AreEqual(0f, bonus,
                "SPK-001: hitting with the opposite side must be worth nothing — this is what makes attachment position matter");
        }

        // B3 — boundary must not flicker
        [Test]
        public void Spk001_ArcBoundary_IsInclusive()
        {
            // Exactly 60 degrees off the weapon axis.
            const float radians = 60f * (float)System.Math.PI / 180f;
            var edge = new Float3((float)System.Math.Sin(radians), (float)System.Math.Cos(radians), 0f);

            Assert.IsTrue(ContactWeaponLogic.InArc(Up, edge, 60f),
                "a contact exactly on the cone boundary must count");
        }

        // B3
        [Test]
        public void Spk001_JustOutsideArc_IsExcluded()
        {
            const float radians = 65f * (float)System.Math.PI / 180f;
            var outside = new Float3((float)System.Math.Sin(radians), (float)System.Math.Cos(radians), 0f);

            Assert.IsFalse(ContactWeaponLogic.InArc(Up, outside, 60f));
        }

        // B5
        [Test]
        public void Spk001_BonusDamage_ScalesWithSpeed()
        {
            float slow = ContactWeaponLogic.BonusDamage(Up, Up, 3f, 12f, 1f, false, Config());
            float fast = ContactWeaponLogic.BonusDamage(Up, Up, 12f, 12f, 1f, false, Config());

            Assert.Greater(fast, slow, "SPK-001: bonus damage is proportional to speed");
        }

        // B5
        [Test]
        public void Spk001_BonusDamage_ScalesWithFrontality()
        {
            float glancing = ContactWeaponLogic.BonusDamage(Up, Up, 12f, 12f, 0.3f, false, Config());
            float headOn = ContactWeaponLogic.BonusDamage(Up, Up, 12f, 12f, 1f, false, Config());

            Assert.Greater(headOn, glancing, "SPK-001: bonus damage is proportional to frontality");
        }

        // B6
        [Test]
        public void Spk001_Dashing_IncreasesBonusDamage()
        {
            float normal = ContactWeaponLogic.BonusDamage(Up, Up, 12f, 12f, 1f, false, Config());
            float dashing = ContactWeaponLogic.BonusDamage(Up, Up, 12f, 12f, 1f, true, Config());

            Assert.That(dashing, Is.EqualTo(normal * 1.5f).Within(1e-3f), "SPK-001: dash increases damage");
        }

        // B6
        [Test]
        public void Spk001_Dashing_IncreasesKnockback()
        {
            float normal = ContactWeaponLogic.KnockbackMultiplier(Up, Up, false, Config());
            float dashing = ContactWeaponLogic.KnockbackMultiplier(Up, Up, true, Config());

            Assert.That(normal, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(dashing, Is.EqualTo(1.4f).Within(1e-4f), "SPK-001: dash increases knockback");
        }

        // B6/B4 — a weapon pointing away must not boost knockback either
        [Test]
        public void Spk001_OutsideArc_DoesNotBoostKnockback()
        {
            var behind = new Float3(0f, -1f, 0f);

            Assert.That(ContactWeaponLogic.KnockbackMultiplier(Up, behind, true, Config()),
                Is.EqualTo(1f).Within(1e-4f));
        }

        // B5 — consistent with DMG-001's speed floor
        [Test]
        public void Spk001_ZeroSpeed_StillHasFloor()
        {
            float bonus = ContactWeaponLogic.BonusDamage(Up, Up, 0f, 12f, 1f, false, Config());

            Assert.That(bonus, Is.EqualTo(8f * 0.25f).Within(1e-3f),
                "DMG-001 consistency: the speed factor has a floor, so a slow spike hit is still worth something");
        }

        // B3 — a degenerate direction cannot be inside any cone
        [Test]
        public void Spk001_ZeroDirection_IsNotInArc()
        {
            Assert.IsFalse(ContactWeaponLogic.InArc(Float3.Zero, Up, 60f));
            Assert.IsFalse(ContactWeaponLogic.InArc(Up, Float3.Zero, 60f));
        }

        // B3 — a full sphere arc covers everything (guards the 180 degree edge case)
        [Test]
        public void Spk001_FullArc_CoversEveryDirection()
        {
            Assert.IsTrue(ContactWeaponLogic.InArc(Up, new Float3(0f, -1f, 0f), 180f));
        }
    }
}
