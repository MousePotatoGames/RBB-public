using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for F05 player health (HP-001 / HP-002).
    /// </summary>
    public sealed class HealthLogicTests
    {
        private static HealthConfig Config => new HealthConfig(maxHealth: 100f, invulnerabilityDuration: 0.45f);

        // B13 — HP-001
        [Test]
        public void Hp001_Damage_ReducesHealth()
        {
            var state = HealthState.Full(Config);

            var result = HealthLogic.ApplyDamage(state, 8f, Config);

            Assert.IsTrue(result.Applied);
            Assert.That(result.State.Current, Is.EqualTo(92f).Within(1e-3f));
            Assert.IsTrue(result.State.IsInvulnerable, "HP-001: a hit must grant invulnerability");
        }

        // B14 — HP-001
        [Test]
        public void Hp001_DuringInvulnerability_DamageIgnored()
        {
            var state = HealthState.Full(Config);
            state = HealthLogic.ApplyDamage(state, 8f, Config).State;

            var second = HealthLogic.ApplyDamage(state, 8f, Config);

            Assert.IsFalse(second.Applied, "HP-001: no damage while invulnerable");
            Assert.That(second.State.Current, Is.EqualTo(92f).Within(1e-3f));
        }

        // B15 — HP-001
        [Test]
        public void Hp001_AfterInvulnerability_DamageAppliesAgain()
        {
            var state = HealthState.Full(Config);
            state = HealthLogic.ApplyDamage(state, 8f, Config).State;

            for (float t = 0f; t < Config.InvulnerabilityDuration + 0.02f; t += 0.02f)
            {
                state = HealthLogic.Tick(state, 0.02f);
            }

            Assert.IsFalse(state.IsInvulnerable, "invulnerability must expire");
            var second = HealthLogic.ApplyDamage(state, 8f, Config);
            Assert.IsTrue(second.Applied);
            Assert.That(second.State.Current, Is.EqualTo(84f).Within(1e-3f));
        }

        // B16 — HP-001
        [Test]
        public void Hp001_Health_NeverGoesBelowZero()
        {
            var state = new HealthState(5f, 0f);

            var result = HealthLogic.ApplyDamage(state, 999f, Config);

            Assert.That(result.State.Current, Is.EqualTo(0f).Within(1e-6f), "HP must clamp at zero");
            Assert.IsFalse(result.State.IsAlive);
        }

        // B16 — HP-001
        [Test]
        public void Hp001_DeathTransition_ReportedOnlyOnce()
        {
            var state = new HealthState(5f, 0f);

            var killing = HealthLogic.ApplyDamage(state, 10f, Config);
            Assert.IsTrue(killing.JustDied, "the killing blow must report the transition");

            var afterDeath = HealthLogic.ApplyDamage(killing.State, 10f, Config);
            Assert.IsFalse(afterDeath.Applied, "a dead player takes no further damage");
            Assert.IsFalse(afterDeath.JustDied, "HP-001: death must be reported once, not repeatedly");
        }

        // 방어 — 음수·0 피해
        [Test]
        public void Hp001_NonPositiveDamage_IsIgnored()
        {
            var state = HealthState.Full(Config);

            var result = HealthLogic.ApplyDamage(state, 0f, Config);

            Assert.IsFalse(result.Applied);
            Assert.That(result.State.Current, Is.EqualTo(Config.MaxHealth).Within(1e-6f));
            Assert.IsFalse(result.State.IsInvulnerable, "a no-op hit must not grant invulnerability");
        }
    }
}
