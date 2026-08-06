using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// TES-001 (B14): a grey line from the ring to whatever it just zapped.
    ///
    /// This is verification equipment, not presentation. Contact and projectile
    /// weapons are visible objects; a discharge is not, so without a line the only
    /// evidence the tesla works is enemy HP quietly dropping — which makes every
    /// manual play check unanswerable. P08 replaces this with real lightning.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ZapVisual : MonoBehaviour
    {
        [SerializeField] private ZapWeaponController controller;

        [Tooltip("F11 임시: 선이 보이는 시간(초). 연출은 P08")]
        [SerializeField] private float flashDuration = 0.08f;

        [SerializeField] private float width = 0.06f;

        private LineRenderer _line;
        private float _hideAt;

        /// <summary>How many flashes were drawn — PlayMode tests assert this is not vacuous.</summary>
        public int FlashCount { get; private set; }

        public ZapWeaponController Controller { get => controller; set => controller = value; }
        public float FlashDuration { get => flashDuration; set => flashDuration = value; }

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = width;
            _line.endWidth = width;
            _line.useWorldSpace = true;
            _line.enabled = false;

            if (controller == null)
            {
                controller = GetComponentInParent<ZapWeaponController>();
            }
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.Zapped += OnZapped;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.Zapped -= OnZapped;
            }

            if (_line != null)
            {
                _line.enabled = false;
            }
        }

        private void OnZapped(Vector3 from, Vector3 to)
        {
            _line.SetPosition(0, from);
            _line.SetPosition(1, to);
            _line.enabled = true;

            // Unscaled: a hit-stop freeze must not stretch the flash into a held beam.
            _hideAt = Time.unscaledTime + flashDuration;
            FlashCount++;
        }

        private void Update()
        {
            if (_line.enabled && Time.unscaledTime >= _hideAt)
            {
                _line.enabled = false;
            }
        }
    }
}
