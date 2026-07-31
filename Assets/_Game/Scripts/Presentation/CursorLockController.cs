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
        private void Update()
        {
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
