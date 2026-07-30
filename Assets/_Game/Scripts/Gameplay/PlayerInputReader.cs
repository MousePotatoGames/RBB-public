using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    /// <summary>
    /// Polls the project-wide Input System "Player/Move" action and feeds
    /// BallMotor. Kept separate from the motor so PlayMode tests can inject
    /// input directly without touching input devices.
    /// </summary>
    [RequireComponent(typeof(BallMotor))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private BallMotor _motor;
        private InputAction _moveAction;

        private void Awake()
        {
            _motor = GetComponent<BallMotor>();
        }

        private void OnEnable()
        {
            _moveAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Move") : null;
            if (_moveAction == null)
            {
                Debug.LogWarning("[F01] Project-wide 'Player/Move' action not found — WASD input will be dead.", this);
                return;
            }

            _moveAction.Enable();
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
        }
    }
}
