using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// LOSE-001 director. Owns the Core session state, listens for the player's
    /// death and the player's kills, and reports the end of the session once
    /// (기획서 14.3의 `GameEnded`). Knows nothing about UI — Presentation
    /// subscribes to <see cref="Ended"/>.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private SessionConfig config;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerDamageDealer damageDealer;

        [Tooltip("F07 임시: 세션 종료를 콘솔로 확인한다 (F13에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole = true;

        private SessionState _state = SessionLogic.Start();
        private bool _endReported;

        /// <summary>GameEnded(state) — raised exactly once per session (B2).</summary>
        public event Action<SessionState> Ended;

        public SessionConfig Config { get => config; set => config = value; }
        public PlayerHealth PlayerHealth { get => playerHealth; set => playerHealth = value; }
        public PlayerDamageDealer DamageDealer { get => damageDealer; set => damageDealer = value; }

        public SessionState State => _state;
        public bool IsRunning => _state.IsRunning;

        /// <summary>How many times <see cref="Ended"/> fired — B2 must keep this at 1.</summary>
        public int EndReportCount { get; private set; }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died += OnPlayerDied;
            }

            if (damageDealer != null)
            {
                damageDealer.Killed += OnEnemyKilled;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDied;
            }

            if (damageDealer != null)
            {
                damageDealer.Killed -= OnEnemyKilled;
            }
        }

        private void Update()
        {
            // Game time, so a hit-stop freeze does not award survival time.
            _state = SessionLogic.Tick(_state, Time.deltaTime);
        }

        /// <summary>
        /// B2/B3: the event is deferred to the end of the frame so a victory
        /// registered later in the same frame can still outrank a defeat before
        /// anyone reads the result. FixedUpdate (where contact damage lands) always
        /// runs before LateUpdate.
        /// </summary>
        private void LateUpdate()
        {
            if (_state.IsRunning || _endReported)
            {
                return;
            }

            _endReported = true;
            EndReportCount++;

            // B5: the field must not keep moving underneath the result screen.
            Time.timeScale = 0f;

            if (logToConsole)
            {
                Debug.Log($"[SESSION] {_state.Outcome} — 생존 {_state.Elapsed:0.0}s / 처치 {_state.Kills}", this);
            }

            Ended?.Invoke(_state);
        }

        private void OnPlayerDied() => End(SessionOutcome.Defeat); // B1

        private void OnEnemyKilled(Vector3 _) => _state = SessionLogic.RegisterKill(_state); // B9

        /// <summary>Ends the session. F13 calls this with Victory when the timer runs out.</summary>
        public void End(SessionOutcome outcome) => _state = SessionLogic.End(_state, outcome);

        /// <summary>Restores real time. Always call before reloading the scene (B11, 기획서 21.3).</summary>
        public static void RestoreTime() => Time.timeScale = 1f;
    }
}
