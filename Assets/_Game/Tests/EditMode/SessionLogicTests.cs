using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// LOSE-001 / FP-001 coverage (Docs/Features/F07-lose-restart.md).
    /// </summary>
    public sealed class SessionLogicTests
    {
        // B1
        [Test]
        public void Lose001_ZeroHealth_EndsSessionAsDefeat()
        {
            SessionState ended = SessionLogic.End(SessionLogic.Start(), SessionOutcome.Defeat);

            Assert.IsFalse(ended.IsRunning);
            Assert.AreEqual(SessionOutcome.Defeat, ended.Outcome);
            Assert.IsTrue(ended.HasEnded);
        }

        // B2 — the result is processed once
        [Test]
        public void Lose001_SecondEnd_IsIgnored()
        {
            SessionState first = SessionLogic.End(SessionLogic.Start(), SessionOutcome.Defeat);
            SessionState second = SessionLogic.End(first, SessionOutcome.Defeat);

            Assert.AreEqual(SessionOutcome.Defeat, second.Outcome);
            Assert.AreEqual(first.Elapsed, second.Elapsed);
            Assert.AreEqual(first.Kills, second.Kills);
        }

        // B3 — LOSE-001 exception
        [Test]
        public void Lose001_VictoryAndDefeat_SameFrame_VictoryWins()
        {
            SessionState defeated = SessionLogic.End(SessionLogic.Start(), SessionOutcome.Defeat);
            SessionState upgraded = SessionLogic.End(defeated, SessionOutcome.Victory);

            Assert.AreEqual(SessionOutcome.Victory, upgraded.Outcome,
                "LOSE-001: a mutual kill must read as a win");
        }

        // B3 — order must not matter
        [Test]
        public void Lose001_DefeatAfterVictory_DoesNotOverride()
        {
            SessionState won = SessionLogic.End(SessionLogic.Start(), SessionOutcome.Victory);
            SessionState after = SessionLogic.End(won, SessionOutcome.Defeat);

            Assert.AreEqual(SessionOutcome.Victory, after.Outcome,
                "LOSE-001: a defeat arriving after a victory must not steal the result");
        }

        // B4
        [Test]
        public void Lose001_ElapsedAccumulates_WhileRunning()
        {
            SessionState state = SessionLogic.Start();
            state = SessionLogic.Tick(state, 0.5f);
            state = SessionLogic.Tick(state, 0.25f);

            Assert.That(state.Elapsed, Is.EqualTo(0.75f).Within(1e-4f));
        }

        // B4
        [Test]
        public void Lose001_ElapsedStops_AfterEnd()
        {
            SessionState state = SessionLogic.Tick(SessionLogic.Start(), 3f);
            state = SessionLogic.End(state, SessionOutcome.Defeat);
            state = SessionLogic.Tick(state, 10f);

            Assert.That(state.Elapsed, Is.EqualTo(3f).Within(1e-4f),
                "B4: survival time must be frozen at the moment of death");
        }

        // B9
        [Test]
        public void Lose001_KillCount_AccumulatesWhileRunning()
        {
            SessionState state = SessionLogic.Start();
            state = SessionLogic.RegisterKill(state);
            state = SessionLogic.RegisterKill(state);

            Assert.AreEqual(2, state.Kills);
        }

        // B9
        [Test]
        public void Lose001_KillsAfterEnd_AreIgnored()
        {
            SessionState state = SessionLogic.RegisterKill(SessionLogic.Start());
            state = SessionLogic.End(state, SessionOutcome.Defeat);
            state = SessionLogic.RegisterKill(state);

            Assert.AreEqual(1, state.Kills, "B9: a kill landing after death must not count");
        }

        // B4 — a fresh session starts clean
        [Test]
        public void Lose001_Start_IsRunningAndEmpty()
        {
            SessionState state = SessionLogic.Start();

            Assert.IsTrue(state.IsRunning);
            Assert.AreEqual(SessionOutcome.None, state.Outcome);
            Assert.That(state.Elapsed, Is.EqualTo(0f).Within(1e-6f));
            Assert.AreEqual(0, state.Kills);
            Assert.IsFalse(state.HasEnded);
        }

        // B2 — a "None" end request is not an end
        [Test]
        public void Lose001_EndWithNoOutcome_KeepsSessionRunning()
        {
            SessionState state = SessionLogic.End(SessionLogic.Start(), SessionOutcome.None);

            Assert.IsTrue(state.IsRunning);
            Assert.AreEqual(SessionOutcome.None, state.Outcome);
        }
    }
}
