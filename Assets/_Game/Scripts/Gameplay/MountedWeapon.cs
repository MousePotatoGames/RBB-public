using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-007 (B1~B6): drives a weapon that moves on its own instead of riding the
    /// ball — orbit or follow.
    ///
    /// Motion is computed in FixedUpdate and <b>rendered interpolated in Update</b>.
    ///
    /// Both halves are needed. Damage judgement runs in FixedUpdate, so the pose it
    /// sees has to be the authoritative one (Decision 0002's "갱신 타이밍" section) —
    /// moving in Update alone would look right and judge wrong. But writing only in
    /// FixedUpdate means the weapon steps 50 times a second while the interpolated
    /// ball moves every rendered frame, and the mismatch reads as the weapon
    /// shivering. So FixedUpdate sets the real pose, Update smooths between the last
    /// two for display, and the next FixedUpdate restores the real one before anything
    /// judges. That is exactly what Rigidbody interpolation does, without needing a
    /// Rigidbody — there is still no collider for physics to care about.
    /// </summary>
    public sealed class MountedWeapon : MonoBehaviour
    {
        /// <summary>Below this much travel per step, direction is noise, not heading.</summary>
        private const float MinTravelForHeading = 0.004f;

        /// <summary>Cap on how fast a follow weapon may swing round, in degrees/second.</summary>
        private const float MaxTurnRate = 540f;

        private WeaponDefinition _definition;
        private Transform _player;
        private float _angle;

        private Vector3 _previousPosition;
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;

        public WeaponDefinition Definition => _definition;

        /// <summary>Current orbit angle in degrees — PlayMode tests read this.</summary>
        public float OrbitAngle => _angle;

        /// <summary>
        /// The authoritative pose, free of render interpolation. Tests assert on this
        /// when they need the value damage judgement actually used.
        /// </summary>
        public Vector3 LogicPosition => _position;

        public void Initialise(WeaponDefinition definition, Transform player, float startAngleDegrees)
        {
            _definition = definition;
            _player = player;
            _angle = startAngleDegrees;
            _position = transform.position;
            _rotation = transform.rotation;

            if (_player != null)
            {
                // Start in position rather than sliding in from wherever it was built.
                // Orbit's Advance(0) already snaps to the circle, but Follow's does not:
                // a zero-length step returns where it is, which is wherever the primitive
                // was created — world origin. Left alone the pet streaks in from the middle
                // of the arena on pickup. Seed it on the same contact direction the orbit
                // angle carries, so where the capsule was touched decides where it appears.
                if (_definition.mount == WeaponMount.Follow)
                {
                    Vector3 c = _player.position;
                    Float3 seed = OrbitLogic.Position(
                        new Float3(c.x, c.y, c.z), _angle, _definition.followStandoff, 0f);
                    _position = new Vector3(seed.X, seed.Y, seed.Z);
                }

                Advance(0f);
                _previousPosition = _position;
                transform.SetPositionAndRotation(_position, _rotation);
            }
        }

        private void FixedUpdate()
        {
            // Decision 0002 flagged this: these weapons are not children of the ball,
            // so a destroyed player leaves a live weapon holding a null reference.
            if (_definition == null || _player == null)
            {
                return;
            }

            _previousPosition = _position;
            Advance(Time.fixedDeltaTime);

            // The pose everything judging in FixedUpdate will read.
            transform.SetPositionAndRotation(_position, _rotation);
        }

        private void Update()
        {
            if (_definition == null || _player == null)
            {
                return;
            }

            float step = Time.fixedDeltaTime;
            float alpha = step > 0f ? Mathf.Clamp01((Time.time - Time.fixedTime) / step) : 1f;

            transform.SetPositionAndRotation(
                Vector3.Lerp(_previousPosition, _position, alpha), _rotation);
        }

        private void Advance(float deltaTime)
        {
            Vector3 p = _player.position;
            var centre = new Float3(p.x, p.y, p.z);

            switch (_definition.mount)
            {
                case WeaponMount.Orbit:
                    _angle = OrbitLogic.Advance(_angle, _definition.orbitAngularSpeed, deltaTime); // B3
                    Float3 orbit = OrbitLogic.Position(
                        centre, _angle, _definition.orbitRadius, _definition.orbitHeight); // B1
                    _position = new Vector3(orbit.X, orbit.Y, orbit.Z);

                    // B2: upright, and untouched by the ball's rotation. Note this puts
                    // the weapon's local +Y — the barrel axis every attack path uses —
                    // straight up, so an orbiting projectile weapon would fire skyward.
                    // No such weapon exists; solve it when one does.
                    _rotation = Quaternion.identity;
                    break;

                case WeaponMount.Follow:
                    var current = new Float3(_position.x, _position.y, _position.z);
                    Float3 desired = FollowLogic.DesiredPosition(current, centre, _definition.followStandoff); // B5
                    Float3 next = FollowLogic.Step(current, desired, _definition.followSpeed, deltaTime); // B4/B6

                    var target = new Vector3(next.X, next.Y, next.Z);
                    Vector3 travel = target - _position;
                    _position = target;

                    // Once settled at the standoff distance the travel per step is
                    // floating-point noise. Turning to face it would spin the weapon at
                    // random every step, which is what "덜덜덜" looks like. Only take a
                    // heading from real movement, and ease into it rather than snapping.
                    if (travel.sqrMagnitude > MinTravelForHeading * MinTravelForHeading)
                    {
                        // Local +Y faces travel — the same convention WeaponSlots uses
                        // for surface weapons, which is what lets the pet reuse the
                        // projectile code untouched (B12).
                        Quaternion heading = Quaternion.FromToRotation(Vector3.up, travel.normalized);
                        _rotation = Quaternion.RotateTowards(_rotation, heading, MaxTurnRate * deltaTime);
                    }

                    break;
            }
        }
    }
}
