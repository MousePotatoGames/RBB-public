using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-009: one straight projectile (B10~B14).
    ///
    /// It carries no collider and no Rigidbody. Hits come from a SphereCast across
    /// the whole step, which is what makes tunnelling impossible — a collider-based
    /// projectile at 18 m/s crosses 0.36 m per physics step and would silently pass
    /// through enemies as soon as the speed was tuned up.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        /// <summary>Enough for a step's worth of overlaps; the array is reused, never allocated per shot.</summary>
        private const int MaxHitsPerStep = 8;

        private static readonly RaycastHit[] Hits = new RaycastHit[MaxHitsPerStep];
        private static readonly int[] Order = new int[MaxHitsPerStep];

        private ProjectilePool _pool;
        private Vector3 _direction;
        private float _speed;
        private float _remainingRange;
        private float _damage;
        private int _remainingPierce;
        private float _radius;

        /// <summary>Enemies already damaged by this shot, so a pierce cannot double-hit one.</summary>
        private readonly List<EnemyHealth> _alreadyHit = new List<EnemyHealth>(4);

        public float RemainingRange => _remainingRange;
        public int RemainingPierce => _remainingPierce;

        /// <summary>Fired from the pool. Resets every piece of per-shot state (B8 reuse).</summary>
        public void Launch(Vector3 origin, Vector3 direction, in ProjectileConfig config, float radius, ProjectilePool pool)
        {
            _pool = pool;
            _direction = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.forward;
            _speed = config.Speed;
            _remainingRange = config.Range;
            _damage = config.Damage;
            _remainingPierce = config.Pierce;
            _radius = radius;
            _alreadyHit.Clear();

            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(_direction, Vector3.up));
        }

        private void FixedUpdate()
        {
            if (_remainingRange <= 0f)
            {
                Expire();
                return;
            }

            float step = _speed * Time.fixedDeltaTime;
            if (step > _remainingRange)
            {
                step = _remainingRange; // B10: never overshoot the configured range
            }

            if (step <= 0f)
            {
                return;
            }

            Vector3 from = transform.position;
            if (Sweep(from, step))
            {
                return; // consumed by a hit
            }

            transform.position = from + _direction * step;
            _remainingRange -= step;

            if (_remainingRange <= 0f)
            {
                Expire();
            }
        }

        /// <summary>
        /// B11~B14: damages enemies along this step in distance order. Returns true
        /// when the projectile was consumed and must not move any further.
        /// </summary>
        private bool Sweep(Vector3 from, float step)
        {
            int count = Physics.SphereCastNonAlloc(
                from, _radius, _direction, Hits, step, ~0, QueryTriggerInteraction.Collide);

            if (count <= 0)
            {
                return false;
            }

            if (count > MaxHitsPerStep)
            {
                count = MaxHitsPerStep;
            }

            SortByDistance(count);
            float now = Time.time;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[Order[i]];

                // Anything without health — ground, walls, capsules — does not stop the
                // shot. Terrain blocking is deliberately out of scope for the flat arena.
                var enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy == null || _alreadyHit.Contains(enemy))
                {
                    continue;
                }

                // B13: the player has no EnemyHealth, so this never fires at us.
                // B12: an enemy inside its DMG-002 cooldown is passed through without
                // spending a pierce — it is not a hit, so it must not cost anything.
                if (!enemy.TryTakeDamage(_damage, now))
                {
                    continue;
                }

                _alreadyHit.Add(enemy);

                if (_remainingPierce <= 0)
                {
                    // Land on the enemy so the (future) impact VFX reads correctly.
                    transform.position = hit.point;
                    Expire();
                    return true;
                }

                _remainingPierce--;
            }

            return false;
        }

        /// <summary>Insertion sort over indices — count is at most 8, so this beats allocating.</summary>
        private static void SortByDistance(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Order[i] = i;
            }

            for (int i = 1; i < count; i++)
            {
                int key = Order[i];
                float keyDistance = Hits[key].distance;
                int j = i - 1;

                while (j >= 0 && Hits[Order[j]].distance > keyDistance)
                {
                    Order[j + 1] = Order[j];
                    j--;
                }

                Order[j + 1] = key;
            }
        }

        private void Expire()
        {
            _remainingRange = 0f;
            if (_pool != null)
            {
                _pool.Release(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
