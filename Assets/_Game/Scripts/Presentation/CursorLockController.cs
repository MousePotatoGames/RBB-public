using Game.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation
{
    /// <summary>
    /// F02 (B7): locks the cursor on click so mouse-orbit camera control works,
    /// releases it on ESC. Editor/desktop behavior — the Web first-click prompt
    /// is a separate concern (P11).
    /// </summary>
    public sealed class CursorLockController : MonoBehaviour
    {
        [Tooltip("F07 (B7): 세션이 끝난 뒤에는 커서를 다시 잠그지 않는다 — 결과 화면을 클릭해야 한다")]
        [SerializeField] private GameSession session;

        public GameSession Session { get => session; set => session = value; }

        private void Update()
        {
            // F07 B7: once the session has ended the result screen owns the cursor.
            if (session != null && !session.IsRunning)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetLocked(false);
                return;
            }

            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            if (clicked && Cursor.lockState != CursorLockMode.Locked)
            {
                SetLocked(true);
            }
        }

        private static void SetLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
