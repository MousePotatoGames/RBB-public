using Game.Core;
using Game.Gameplay;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// Hit stop (기획서 11.2): freezes time briefly on a solid hit so impacts read
    /// as impacts. Lives in Presentation and subscribes to the Gameplay dealer, so
    /// the dependency arrow stays Presentation → Gameplay.
    /// Time.timeScale is always restored — including on disable (B17, B18).
    /// </summary>
    public sealed class HitStopController : MonoBehaviour
    {
        [SerializeField] private PlayerDamageDealer dealer;
        [SerializeField] private DroneConfig config;

        private float _remaining;
        private bool _frozen;
        private float _restoreScale = 1f;

        public bool IsFrozen => _frozen;
        public float Remaining => _remaining;

        /// <summary>
        /// How many freezes have started. A freeze lasts 0.05~0.09s, which is far
        /// too short to catch by sampling — this makes B15/B17 observable at
        /// runtime (F13 replaces it with real feedback).
        /// </summary>
        public int FreezeCount { get; private set; }

        public PlayerDamageDealer Dealer { get => dealer; set => dealer = value; }
        public DroneConfig Config { get => config; set => config = value; }

        private void Awake()
        {
            if (dealer == null)
            {
                dealer = GetComponent<PlayerDamageDealer>();
            }
        }

        private void OnEnable()
        {
            if (dealer != null)
            {
                dealer.Hit += OnHit;
            }
        }

        private void OnDisable()
        {
            if (dealer != null)
            {
                dealer.Hit -= OnHit;
            }

            // B18: never leave the game frozen if this component goes away.
            Release();
        }

        private void OnHit(float damage, Vector3 _)
        {
            if (config == null)
            {
                return;
            }

            float duration = HitStopLogic.DurationFor(damage, config.ToHitStopConfig());
            if (duration <= 0f)
            {
                return; // B15
            }

            _remaining = HitStopLogic.Merge(_remaining, duration); // B16
            if (!_frozen)
            {
                _restoreScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                _frozen = true;
                FreezeCount++;
            }
        }

        private void Update()
        {
            if (!_frozen)
            {
                return;
            }

            // B17: the freeze is measured in real time, since scaled time is stopped.
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                Release();
            }
        }

        /// <summary>Ends the freeze and restores the time scale.</summary>
        public void Release()
        {
            _remaining = 0f;
            if (!_frozen)
            {
                return;
            }

            Time.timeScale = _restoreScale <= 0f ? 1f : _restoreScale;
            _frozen = false;
        }
    }
}
