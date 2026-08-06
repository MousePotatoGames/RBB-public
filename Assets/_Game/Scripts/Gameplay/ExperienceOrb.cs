using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// XP-001 (B3~B5): one dropped experience orb.
    ///
    /// No collider, no Rigidbody — the same judgement as F09~F12. Distance is
    /// checked directly, which also means the orb cannot shove the ball on the
    /// frame it is pooled back in.
    ///
    /// Motion runs in <c>Update</c>, not <c>FixedUpdate</c>: the orb chases the
    /// <i>rendered</i> ball, nothing physical reads its position, and moving every
    /// rendered frame sidesteps the mismatch that made F12's mounts shiver.
    /// Time.deltaTime is zero while LVL-001 has time stopped, so orbs freeze with
    /// everything else (B10) for free.
    /// </summary>
    public sealed class ExperienceOrb : MonoBehaviour
    {
        private ProgressConfig _config;
        private Transform _player;
        private ExperienceOrbPool _pool;
        private bool _live;

        /// <summary>How much XP this orb carries (XP-002).</summary>
        public float Value { get; private set; }

        /// <summary>True once it has entered the magnet radius — it never lets go again.</summary>
        public bool IsChasing { get; private set; }

        public bool IsLive => _live;

        public void Launch(Vector3 position, float value, Transform player, ProgressConfig config, ExperienceOrbPool pool)
        {
            transform.position = position;
            Value = value;
            _player = player;
            _config = config;
            _pool = pool;
            _live = true;
            IsChasing = false;
        }

        /// <summary>Called by the pool when the orb is recalled without being collected (B6).</summary>
        public void Deactivate()
        {
            _live = false;
            IsChasing = false;
        }

        private void Update()
        {
            if (!_live || _player == null || _config == null)
            {
                return;
            }

            // LVL-001 Exception (B10): while time is stopped nothing advances — and
            // that includes being swallowed. Without this an orb already sitting
            // inside the absorb distance would still be collected during the card
            // screen, awarding XP in a frame the rule says is frozen.
            if (Time.deltaTime <= 0f)
            {
                return;
            }

            Vector3 p = _player.position;
            var orb = new Float3(transform.position.x, transform.position.y, transform.position.z);
            var target = new Float3(p.x, p.y, p.z);

            // PAS-003 (B23): the pool owns the multiplier so orbs stay ignorant of passives.
            float radius = _pool != null ? _pool.EffectiveMagnetRadius : _config.magnetRadius;

            // B3: once pulled in it stays pulled in. Without this an orb dropped at
            // the edge would stutter in and out of range as the ball jitters around
            // the boundary.
            if (!IsChasing && !MagnetLogic.InRange(orb, target, radius))
            {
                return;
            }

            IsChasing = true;

            Float3 next = MagnetLogic.Step(orb, target, _config.orbSpeed, Time.deltaTime);
            transform.position = new Vector3(next.X, next.Y, next.Z);

            if (MagnetLogic.IsAbsorbed(next, target, _config.absorbDistance))
            {
                _live = false;
                _pool?.Collect(this); // B4
            }
        }
    }
}
