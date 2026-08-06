using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-007 Exception (B14): the stationary parent for orbit and follow weapons.
    ///
    /// These weapons are not children of the ball — that is the whole point of the
    /// mount — which means they outlive the player unless something cleans them up.
    /// One container makes that a single Destroy instead of per-weapon bookkeeping.
    ///
    /// The container never moves. Each weapon reads the player's position itself, so
    /// there is no update-order dependency between them (Decision 0002).
    /// </summary>
    public sealed class WeaponContainer : MonoBehaviour
    {
        [SerializeField] private GameSession session;

        public GameSession Session { get => session; set => session = value; }

        /// <summary>Weapons currently mounted here — PlayMode tests assert this reaches 0.</summary>
        public int MountedCount => transform.childCount;

        private void OnEnable()
        {
            if (session != null)
            {
                session.Ended += OnSessionEnded;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.Ended -= OnSessionEnded;
            }
        }

        private void OnSessionEnded(SessionState _) => ClearAll();

        /// <summary>B14: removes every mounted weapon. Used on session end and by tests.</summary>
        public void ClearAll()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
