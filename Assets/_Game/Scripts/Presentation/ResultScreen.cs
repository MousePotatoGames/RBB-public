using Game.Core;
using Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// LOSE-001 result screen: survival time, kill count, one-click retry.
    /// Subscribes to <see cref="GameSession.Ended"/> so the dependency arrow
    /// stays Presentation → Gameplay.
    ///
    /// Only the fields that exist today are shown (B8). Best combo (F13),
    /// level (F12) and weapons (F08~F11) get added by those features.
    /// </summary>
    public sealed class ResultScreen : MonoBehaviour
    {
        [SerializeField] private SessionConfig config;
        [SerializeField] private GameSession session;

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI outcomeLabel;
        [SerializeField] private TextMeshProUGUI statsLabel;
        [SerializeField] private Button retryButton;

        private bool _pending;
        private float _showAt;

        public SessionConfig Config { get => config; set => config = value; }
        public GameSession Session { get => session; set => session = value; }
        public GameObject Panel { get => panel; set => panel = value; }
        public TextMeshProUGUI OutcomeLabel { get => outcomeLabel; set => outcomeLabel = value; }
        public TextMeshProUGUI StatsLabel { get => statsLabel; set => statsLabel = value; }
        public Button RetryButton { get => retryButton; set => retryButton = value; }

        /// <summary>True once the screen is actually on screen (B7/B13).</summary>
        public bool IsShown => panel != null && panel.activeSelf;

        private void Awake()
        {
            Hide(); // B13
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.Ended += OnSessionEnded;
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(Retry);
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.Ended -= OnSessionEnded;
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(Retry);
            }
        }

        private void Hide()
        {
            _pending = false;
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnSessionEnded(SessionState state)
        {
            if (outcomeLabel != null)
            {
                outcomeLabel.text = state.Outcome == SessionOutcome.Victory ? "VICTORY" : "DEFEAT";
            }

            if (statsLabel != null)
            {
                // ASCII only: the default TMP font has no Hangul glyphs, and shipping a
                // Korean atlas costs far more than the 50MB WebGL budget allows (기획서 15장).
                statsLabel.text = $"TIME  {state.Elapsed:0.0}s\nKILLS  {state.Kills}";
            }

            // Real time: game time is stopped by now (B5).
            _pending = true;
            _showAt = Time.unscaledTime + (config != null ? config.resultDelaySeconds : 0f);
        }

        private void Update()
        {
            if (_pending && Time.unscaledTime >= _showAt)
            {
                Show();
            }

            if (IsShown && RetryPressed())
            {
                Retry(); // B10
            }
        }

        private void Show()
        {
            _pending = false;
            if (panel != null)
            {
                panel.SetActive(true);
            }

            // B7: a locked cursor cannot click the button.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static bool RetryPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard.rKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        }

        /// <summary>B11: restore real time *before* the reload — LoadScene will not do it.</summary>
        public void Retry()
        {
            GameSession.RestoreTime();
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex >= 0 ? active.name : active.path);
        }
    }
}
