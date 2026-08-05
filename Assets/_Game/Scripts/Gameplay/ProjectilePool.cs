using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-009 (B8/B9/B16): the only place projectiles are created or destroyed.
    ///
    /// Straight projectiles must be pooled (기획서 15장), and buckshot × fire rate
    /// could drain any pool, so a live cap sits in front of it. Hitting the cap
    /// skips the shot rather than growing the pool.
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [Tooltip("WPN-009 Exception: 동시에 살아있을 수 있는 투사체 수. 넘으면 발사를 건너뛴다")]
        [SerializeField] private int liveCap = 32;

        [Tooltip("세션이 끝나면 남은 투사체를 정리한다 (B16)")]
        [SerializeField] private GameSession session;

        [Header("Visual (greybox — P07 replaces this)")]
        [SerializeField] private PrimitiveType shape = PrimitiveType.Sphere;

        [SerializeField] private float scale = 0.18f;

        private ObjectPool<Projectile> _pool;
        private readonly List<Projectile> _live = new List<Projectile>();

        public int LiveCount => _live.Count;

        /// <summary>How many objects were ever instantiated — B8 asserts this stays well under the shot count.</summary>
        public int TotalCreated { get; private set; }

        public int LiveCap { get => liveCap; set => liveCap = value; }
        public GameSession Session { get => session; set => session = value; }

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

        /// <summary>B8/B9: launches one projectile, or returns false when the cap is reached.</summary>
        public bool TryLaunch(Vector3 origin, Vector3 direction, in ProjectileConfig config, float radius)
        {
            if (_live.Count >= liveCap)
            {
                return false;
            }

            EnsurePool();
            Projectile projectile = _pool.Get();
            _live.Add(projectile);
            projectile.Launch(origin, direction, config, radius, this);
            return true;
        }

        /// <summary>Returns a spent projectile. Safe to call twice — the second call is a no-op.</summary>
        public void Release(Projectile projectile)
        {
            if (projectile == null || _pool == null)
            {
                return;
            }

            if (!_live.Remove(projectile))
            {
                return; // already released — never double-release into the pool
            }

            _pool.Release(projectile);
        }

        /// <summary>B16: clears the field. Used on session end and by tests.</summary>
        public void ClearAll()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                _pool?.Release(_live[i]);
            }

            _live.Clear();
        }

        private void EnsurePool()
        {
            if (_pool != null)
            {
                return;
            }

            _pool = new ObjectPool<Projectile>(
                createFunc: Create,
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: true,
                defaultCapacity: 16,
                maxSize: 64);
        }

        private Projectile Create()
        {
            TotalCreated++;

            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = "Projectile";

            // Visual only. DestroyImmediate, not Destroy: a deferred destroy leaves the
            // collider alive for one physics step, which is long enough for a freshly
            // pooled projectile to shove the ball (same reason as WeaponSlots).
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * scale;
            go.SetActive(false);

            return go.AddComponent<Projectile>();
        }
    }
}
