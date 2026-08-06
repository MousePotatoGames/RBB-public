using Game.Core;
using Game.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// LVL-001 (B11, B14): the passive cards.
    ///
    /// Reads <see cref="LevelUpDirector"/> and reports a choice back to it —
    /// Presentation → Gameplay, the same shape as ResultScreen.
    ///
    /// Everything here runs on unscaled time and frame-based input, because the
    /// director has stopped game time by the time this is visible (B10).
    /// </summary>
    public sealed class LevelUpScreen : MonoBehaviour
    {
        [SerializeField] private LevelUpDirector director;

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private Button[] cardButtons = new Button[0];
        [SerializeField] private TextMeshProUGUI[] cardLabels = new TextMeshProUGUI[0];

        private CursorLockMode _restoreLock = CursorLockMode.Locked;
        private bool _restoreVisible;

        public LevelUpDirector Director { get => director; set => director = value; }
        public GameObject Panel { get => panel; set => panel = value; }
        public TextMeshProUGUI TitleLabel { get => titleLabel; set => titleLabel = value; }
        public Button[] CardButtons { get => cardButtons; set => cardButtons = value; }
        public TextMeshProUGUI[] CardLabels { get => cardLabels; set => cardLabels = value; }

        public bool IsShown => panel != null && panel.activeSelf;

        private void Awake() => Hide();

        private void OnEnable()
        {
            if (director != null)
            {
                director.Opened += Show;
                director.Closed += Hide;
            }

            BindButtons(true);
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.Opened -= Show;
                director.Closed -= Hide;
            }

            BindButtons(false);
        }

        private void BindButtons(bool bind)
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] == null)
                {
                    continue;
                }

                int index = i; // capture per button
                if (bind)
                {
                    cardButtons[i].onClick.AddListener(() => Choose(index));
                }
                else
                {
                    cardButtons[i].onClick.RemoveAllListeners();
                }
            }
        }

        private void Show()
        {
            if (director == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                // ASCII: the default TMP font has no Hangul glyphs (설계 판단 6).
                titleLabel.text = "LEVEL UP";
            }

            int count = director.CandidateCount;

            for (int i = 0; i < cardButtons.Length; i++)
            {
                bool used = i < count;

                if (cardButtons[i] != null)
                {
                    cardButtons[i].gameObject.SetActive(used);
                }

                if (used && i < cardLabels.Length && cardLabels[i] != null)
                {
                    cardLabels[i].text = CardText(director.CandidateAt(i), i);
                }
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }

            // The pointer has to reach the cards. CursorLockController stands down
            // while the director reports IsOpen, so this is not fought over.
            _restoreLock = Cursor.lockState;
            _restoreVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private string CardText(PassiveDefinition definition, int index)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            int stage = director.StageOf(definition.kind);
            string pips = PassiveDefinition.StagePips(stage, PassiveLogic.MaxStage);

            return $"{definition.displayName}\n{pips}\n\n{definition.EffectLines()}\n\n[{index + 1}]";
        }

        private void Hide()
        {
            if (panel != null && panel.activeSelf)
            {
                Cursor.lockState = _restoreLock;
                Cursor.visible = _restoreVisible;
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsShown || director == null || !director.IsOpen)
            {
                return;
            }

            int pressed = PressedIndex();
            if (pressed >= 0)
            {
                Choose(pressed);
            }
        }

        /// <summary>B11: keyboard 1/2/3, matching the [1] [2] [3] on the cards.</summary>
        private int PressedIndex()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return -1;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) return 0;
            if (keyboard.digit2Key.wasPressedThisFrame) return 1;
            if (keyboard.digit3Key.wasPressedThisFrame) return 2;

            return -1;
        }

        public void Choose(int index)
        {
            if (director == null || !director.IsOpen || index < 0 || index >= director.CandidateCount)
            {
                return;
            }

            director.Choose(index); // B12: the director resumes time and raises Closed
        }
    }
}
