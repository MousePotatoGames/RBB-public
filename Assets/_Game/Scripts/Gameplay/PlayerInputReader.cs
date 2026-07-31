using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    /// <summary>
    /// Polls the project-wide Input System actions (Move / Jump / Sprint) and
    /// feeds BallMotor. Kept separate from the motor so PlayMode tests can
    /// inject input directly without touching input devices.
    /// </summary>
    [RequireComponent(typeof(BallMotor))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private BallMotor _motor;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _dashAction;

        private void Awake()
        {
            _motor = GetComponent<BallMotor>();
        }

        private void OnEnable()
        {
            _moveAction = Resolve("Player/Move");
            _jumpAction = Resolve("Player/Jump");
            _dashAction = Resolve("Player/Sprint");
        }

        private void OnDisable()
        {
            if (_motor != null)
            {
                _motor.SetMoveInput(Vector2.zero);
            }
        }

        private void Update()
        {
            if (_moveAction != null)
            {
                _motor.SetMoveInput(_moveAction.ReadValue<Vector2>());
            }

            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
            {
                _motor.QueueJump();
            }

            if (_dashAction != null && _dashAction.WasPressedThisFrame())
            {
                _motor.QueueDash();
            }
        }

        private static InputAction Resolve(string path)
        {
            InputAction action = InputSystem.actions != null ? InputSystem.actions.FindAction(path) : null;
            if (action == null)
            {
                Debug.LogWarning($"[F03] Project-wide action '{path}' not found — that input will be dead.");
                return null;
            }

            action.Enable();
            return action;
        }
    }
}
