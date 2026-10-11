using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// LVL-001 (B10~B16, B20): stops time, offers the cards, resumes.
    ///
    /// The screen lives in Presentation and only reads this — the dependency arrow
    /// stays Presentation → Gameplay, the same shape as ResultScreen.
    /// </summary>
    public sealed class LevelUpDirector : MonoBehaviour
    {
        [SerializeField] private ProgressConfig config;
        [SerializeField] private PlayerProgress progress;
        [SerializeField] private GameSession session;

        [Tooltip("PAS-001~003 에셋 3개. 순서가 곧 카드 순서다")]
        [SerializeField] private List<PassiveDefinition> passives = new List<PassiveDefinition>();

        [Tooltip("F13 임시: 카드 표시를 콘솔로 확인한다")]
        [SerializeField] private bool logToConsole = true;

        private readonly PassiveKind[] _candidateKinds = new PassiveKind[8];
        private readonly List<PassiveDefinition> _candidates = new List<PassiveDefinition>();

        private int _queued;
        private bool _frozeTime;

        /// <summary>Raised when the cards should appear. The screen reads the candidates.</summary>
        public event Action Opened;

        /// <summary>Raised after a choice is taken and time is running again.</summary>
        public event Action Closed;

        public ProgressConfig Config { get => config; set => config = value; }
        public PlayerProgress Progress { get => progress; set => progress = value; }
        public GameSession Session { get => session; set => session = value; }
        public List<PassiveDefinition> Passives { get => passives; set => passives = value; }

        public bool IsOpen { get; private set; }

        /// <summary>Level-ups waiting for a card (B16).</summary>
        public int QueuedLevelUps => _queued;

        /// <summary>How many times cards were actually shown — tests assert on this.</summary>
        public int OpenCount { get; private set; }

        public int CandidateCount => _candidates.Count;

        public PassiveDefinition CandidateAt(int index) =>
            index >= 0 && index < _candidates.Count ? _candidates[index] : null;

        /// <summary>Current stage of a candidate, for the card's pips.</summary>
        public int StageOf(PassiveKind kind) => progress != null ? progress.StageOf(kind) : 0;

        private void OnEnable()
        {
            if (progress != null)
            {
                progress.LevelUp += OnLevelUp;
            }

            if (session != null)
            {
                session.Ended += OnSessionEnded;
            }
        }

        private void OnDisable()
        {
            if (progress != null)
            {
                progress.LevelUp -= OnLevelUp;
            }

            if (session != null)
            {
                session.Ended -= OnSessionEnded;
            }

            // A director destroyed while the cards are up would leave the game frozen
            // with nothing left to unfreeze it.
            Thaw();
            IsOpen = false;
        }

        private void OnLevelUp(int _)
        {
            _queued++;
            TryOpen();
        }

        /// <summary>B15: the result screen owns the end of the session, not this.</summary>
        private void OnSessionEnded(SessionState _)
        {
            _queued = 0;

            if (IsOpen)
            {
                IsOpen = false;
                _frozeTime = false; // GameSession stopped time on purpose — leave it stopped
                Closed?.Invoke();
            }
        }

        private void TryOpen()
        {
            if (IsOpen || _queued <= 0)
            {
                return;
            }

            if (session != null && !session.IsRunning)
            {
                _queued = 0; // B15
                return;
            }

            BuildCandidates();

            // B20: PAS-004's exception. Freezing the game for an empty screen would
            // hand the player a pause they cannot end.
            if (_candidates.Count == 0)
            {
                _queued = 0;
                return;
            }

            IsOpen = true;
            OpenCount++;
            Freeze(); // B10

            if (logToConsole)
            {
                Debug.Log($"[LVL] cards ×{_candidates.Count} (queued {_queued})", this);
            }

            Opened?.Invoke();
        }

        private void BuildCandidates()
        {
            _candidates.Clear();

            PassiveState state = progress != null ? progress.Passives : PassiveState.Empty;
            int count = PassiveLogic.Candidates(state, _candidateKinds);

            for (int i = 0; i < count; i++)
            {
                PassiveDefinition definition = Find(_candidateKinds[i]);
                if (definition != null)
                {
                    _candidates.Add(definition);
                }
            }
        }

        private PassiveDefinition Find(PassiveKind kind)
        {
            for (int i = 0; i < passives.Count; i++)
            {
                if (passives[i] != null && passives[i].kind == kind)
                {
                    return passives[i];
                }
            }

            return null;
        }

        /// <summary>B11/B12: takes the chosen card and gets the game moving again.</summary>
        public bool Choose(int index)
        {
            if (!IsOpen)
            {
                return false;
            }

            PassiveDefinition chosen = CandidateAt(index);
            if (chosen == null)
            {
                return false;
            }

            progress?.TakePassive(chosen.kind);

            IsOpen = false;
            _queued--;
            if (_queued < 0)
            {
                _queued = 0;
            }

            Thaw();
            Closed?.Invoke();

            // B16: a double level-up owes a second card, and it opens immediately —
            // LVL-001's 0.2초 budget is per resume, not per level.
            TryOpen();
            return true;
        }

        private void Freeze()
        {
            _frozeTime = true;
            Time.timeScale = 0f;
        }

        private void Thaw()
        {
            if (!_frozeTime)
            {
                return;
            }

            _frozeTime = false;
            Time.timeScale = 1f;
        }
    }
}
