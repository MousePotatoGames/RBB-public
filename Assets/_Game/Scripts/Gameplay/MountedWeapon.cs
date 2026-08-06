using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-007 (B1~B6): drives a weapon that moves on its own instead of riding the
    /// ball — orbit or follow.
    ///
    /// Runs in FixedUpdate on purpose. Damage judgement happens in FixedUpdate too,
    /// so moving here keeps the position the judgement sees identical to the position
    /// the weapon actually had (Decision 0002's "갱신 타이밍" section). Updating in
    /// Update would look right and judge wrong.
    ///
    /// No Rigidbody and no collider: with radius-based judgement there is nothing for
    /// the physics engine to do. The cost is no interpolation — see the spec's open
    /// questions about low frame rates.
    /// </summary>
    public sealed class MountedWeapon : MonoBehaviour
    {
        private WeaponDefinition _definition;
        private Transform _player;
        private float _angle;

        public WeaponDefinition Definition => _definition;

        /// <summary>Current orbit angle in degrees — PlayMode tests read this.</summary>
        public float OrbitAngle => _angle;

        public void Initialise(WeaponDefinition definition, Transform player, float startAngleDegrees)
        {
            _definition = definition;
            _player = player;
            _angle = startAngleDegrees;

            if (_player != null)
            {
                Place(0f); // start in position rather than sliding in from the origin
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

            Place(Time.fixedDeltaTime);
        }

        private void Place(float deltaTime)
        {
            Vector3 p = _player.position;
            var centre = new Float3(p.x, p.y, p.z);

            switch (_definition.mount)
            {
                case WeaponMount.Orbit:
                    _angle = OrbitLogic.Advance(_angle, _definition.orbitAngularSpeed, deltaTime); // B3
                    Float3 orbit = OrbitLogic.Position(
                        centre, _angle, _definition.orbitRadius, _definition.orbitHeight); // B1
                    transform.position = new Vector3(orbit.X, orbit.Y, orbit.Z);

                    // B2: upright, and untouched by the ball's rotation. Note this puts
                    // the weapon's local +Y — the barrel axis every attack path uses —
                    // straight up, so an orbiting projectile weapon would fire skyward.
                    // No such weapon exists; solve it when one does.
                    transform.rotation = Quaternion.identity;
                    break;

                case WeaponMount.Follow:
                    var current = new Float3(transform.position.x, transform.position.y, transform.position.z);
                    Float3 desired = FollowLogic.DesiredPosition(current, centre, _definition.followStandoff); // B5
                    Float3 next = FollowLogic.Step(current, desired, _definition.followSpeed, deltaTime); // B4/B6

                    var target = new Vector3(next.X, next.Y, next.Z);
                    Vector3 travel = target - transform.position;
                    transform.position = target;

                    // Local +Y faces the direction of travel — the same convention
                    // WeaponSlots uses for surface weapons, which is what lets the pet
                    // reuse the projectile code untouched (B12).
                    if (travel.sqrMagnitude > 1e-8f)
                    {
                        transform.rotation = Quaternion.FromToRotation(Vector3.up, travel.normalized);
                    }

                    break;
            }
        }
    }
}
