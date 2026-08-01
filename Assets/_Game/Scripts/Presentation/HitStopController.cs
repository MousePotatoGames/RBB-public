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

        [Tooltip("F07 (B6): 세션이 끝나면 정지 소유권을 넘기고 물러난다")]
        [SerializeField] private GameSession session;

        private float _remaining;
        private bool _frozen;
        private float _restoreScale = 1f;
        private float _lastFreezeTime = HitStopLogic.NeverFroze;

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
        public GameSession Session { get => session; set => session = value; }

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

            if (session != null)
            {
                session.Ended += OnSessionEnded;
            }
        }

        private void OnDisable()
        {
            if (dealer != null)
            {
                dealer.Hit -= OnHit;
            }

            if (session != null)
            {
                session.Ended -= OnSessionEnded;
            }

            // B18: never leave the game frozen if this component goes away.
            Release();
        }

        /// <summary>
        /// F07 (B6): the session owns Time.timeScale once it has ended. Drop the
        /// freeze *without* restoring the scale — restoring it here would undo the
        /// session pause — then stop listening.
        /// </summary>
        private void OnSessionEnded(SessionState _)
        {
            _frozen = false;
            _remaining = 0f;
            enabled = false;
        }

        private void OnHit(float damage, Vector3 _)
        {
            if (config == null)
            {
                return;
            }

            HitStopConfig hitStop = config.ToHitStopConfig();

            float duration = HitStopLogic.DurationFor(damage, hitStop);
            if (duration <= 0f)
            {
                return; // B15
            }

            if (_frozen)
            {
                _remaining = HitStopLogic.Merge(_remaining, duration); // B16: simultaneous hits never stack
                return;
            }

            // DMG-005 (B20): sequential hits in a swarm must not chain into stutter.
            // Real time, because scaled time is stopped during a freeze.
            if (!HitStopLogic.CanFreeze(_lastFreezeTime, Time.unscaledTime, hitStop.Refractory))
            {
                return;
            }

            _remaining = duration;
            _restoreScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _frozen = true;
            _lastFreezeTime = Time.unscaledTime;
            FreezeCount++;
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
