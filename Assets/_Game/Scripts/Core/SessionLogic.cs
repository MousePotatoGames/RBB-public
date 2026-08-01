namespace Game.Core
{
    /// <summary>How a session ended. None = still running (LOSE-001 / FP-001).</summary>
    public enum SessionOutcome
    {
        None = 0,
        Defeat = 1,

        /// <summary>
        /// Higher than Defeat on purpose — LOSE-001's exception says a victory in
        /// the same frame wins. F13 raises it; F07 only reserves the seat.
        /// </summary>
        Victory = 2,
    }

    /// <summary>Immutable snapshot of one play session.</summary>
    public readonly struct SessionState
    {
        public readonly bool IsRunning;
        public readonly SessionOutcome Outcome;

        /// <summary>Survival time in game seconds. Frozen once the session ends (B4).</summary>
        public readonly float Elapsed;

        public readonly int Kills;

        public SessionState(bool isRunning, SessionOutcome outcome, float elapsed, int kills)
        {
            IsRunning = isRunning;
            Outcome = outcome;
            Elapsed = elapsed;
            Kills = kills;
        }

        public bool HasEnded => !IsRunning && Outcome != SessionOutcome.None;
    }

    /// <summary>
    /// LOSE-001 / FP-001 session state machine, engine-free so "ends exactly
    /// once" and "victory beats defeat" are testable without play mode.
    /// </summary>
    public static class SessionLogic
    {
        public static SessionState Start() => new SessionState(true, SessionOutcome.None, 0f, 0);

        /// <summary>B4: time only accumulates while the session is running.</summary>
        public static SessionState Tick(in SessionState state, float deltaTime)
        {
            if (!state.IsRunning || deltaTime <= 0f)
            {
                return state;
            }

            return new SessionState(true, state.Outcome, state.Elapsed + deltaTime, state.Kills);
        }

        /// <summary>B9: kills scored after the session ended do not count.</summary>
        public static SessionState RegisterKill(in SessionState state)
        {
            if (!state.IsRunning)
            {
                return state;
            }

            return new SessionState(true, state.Outcome, state.Elapsed, state.Kills + 1);
        }

        /// <summary>
        /// B2/B3: ends the session. A second end is ignored unless it outranks
        /// the recorded one — Victory beats Defeat whichever arrives first, so a
        /// mutual kill reads as a win (LOSE-001 exception).
        /// </summary>
        public static SessionState End(in SessionState state, SessionOutcome outcome)
        {
            if (outcome == SessionOutcome.None)
            {
                return state;
            }

            if (!state.IsRunning && outcome <= state.Outcome)
            {
                return state; // already ended, and this is not a stronger result
            }

            return new SessionState(false, outcome, state.Elapsed, state.Kills);
        }
    }
}
