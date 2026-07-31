using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for F05 spawning, chasing and separation
    /// (Docs/Features/F05-drone-spawner.md). Names carry the GAME_RULES id.
    /// </summary>
    public sealed class SpawnAndChaseLogicTests
    {
        private static SpawnBudgetConfig Budget =>
            new SpawnBudgetConfig(startCount: 5, endCount: 25, rampSeconds: 90f, hardCap: 30, spawnInterval: 0.35f);

        private static Float3 CamForward => new Float3(0f, 0f, 1f);

        // B3 — WAVE-001
        [Test]
        public void Wave001_TargetCount_RampsLinearlyOverTime()
        {
            Assert.AreEqual(5, SpawnBudgetLogic.TargetCount(0f, Budget));
            Assert.AreEqual(15, SpawnBudgetLogic.TargetCount(45f, Budget), "midpoint of a 5→25 ramp");
            Assert.AreEqual(25, SpawnBudgetLogic.TargetCount(90f, Budget));
        }

        // B3 — WAVE-001
        [Test]
        public void Wave001_TargetCount_ClampsAfterSessionEnd()
        {
            Assert.AreEqual(25, SpawnBudgetLogic.TargetCount(200f, Budget), "target must hold after the ramp");
        }

        // B4 — WAVE-001
        [Test]
        public void Wave001_ShouldSpawn_OnlyWhenBelowTarget()
        {
            Assert.IsTrue(SpawnBudgetLogic.ShouldSpawn(4, 10, 1f, Budget));
            Assert.IsFalse(SpawnBudgetLogic.ShouldSpawn(10, 10, 1f, Budget), "at target: no spawn");
            Assert.IsFalse(SpawnBudgetLogic.ShouldSpawn(12, 10, 1f, Budget), "above target: no spawn");
        }

        // B4 — WAVE-001
        [Test]
        public void Wave001_ShouldSpawn_RespectsInterval()
        {
            Assert.IsFalse(SpawnBudgetLogic.ShouldSpawn(0, 10, 0.1f, Budget), "interval not elapsed");
            Assert.IsTrue(SpawnBudgetLogic.ShouldSpawn(0, 10, 0.35f, Budget));
        }

        // B5 — WAVE-001 exception
        [Test]
        public void Wave001_ShouldSpawn_NeverExceedsHardCap()
        {
            Assert.IsFalse(SpawnBudgetLogic.ShouldSpawn(30, 999, 10f, Budget),
                "WAVE-001: hard cap wins over the target count");
        }

        // B6 — WAVE-002
        [Test]
        public void Wave002_SpawnDirection_IsUnitAndPlanar()
        {
            for (float angle = 0f; angle < 360f; angle += 45f)
            {
                Float3 dir = SpawnRingLogic.SpawnDirection(angle);
                Assert.That(dir.Magnitude(), Is.EqualTo(1f).Within(1e-3f), $"angle {angle}");
                Assert.That(dir.Y, Is.EqualTo(0f).Within(1e-6f), "spawn direction must stay planar");
            }
        }

        // B6 — WAVE-002
        [Test]
        public void Wave002_SpawnPosition_AvoidsCameraFrontCone()
        {
            const float half = 55f;
            float angle = SpawnRingLogic.FirstAngleOutsideCone(0f, 137f, CamForward, half);
            Float3 dir = SpawnRingLogic.SpawnDirection(angle);

            Assert.IsFalse(SpawnRingLogic.IsInsideCameraCone(dir, CamForward, half),
                "WAVE-002: spawns must not appear inside the camera's front cone");
        }

        // B6 — WAVE-002 (cone detection itself)
        [Test]
        public void Wave002_CameraCone_DetectsFrontAndBack()
        {
            Assert.IsTrue(SpawnRingLogic.IsInsideCameraCone(new Float3(0f, 0f, 1f), CamForward, 55f), "dead ahead is inside");
            Assert.IsFalse(SpawnRingLogic.IsInsideCameraCone(new Float3(0f, 0f, -1f), CamForward, 55f), "behind is outside");
            Assert.IsFalse(SpawnRingLogic.IsInsideCameraCone(new Float3(1f, 0f, 0f), CamForward, 55f), "90° is outside a 55° cone");
        }

        // B7 — WAVE-002
        [Test]
        public void Wave002_NextAngle_DistributesAcrossRing()
        {
            // 8 successive angles with the golden step must land in at least 3 quadrants.
            var quadrants = new System.Collections.Generic.HashSet<int>();
            float angle = 0f;
            for (int i = 0; i < 8; i++)
            {
                quadrants.Add((int)(angle / 90f) % 4);
                angle = SpawnRingLogic.NextAngle(angle, 137f);
                Assert.That(angle, Is.InRange(0f, 360f), "angle must stay normalised");
            }

            Assert.GreaterOrEqual(quadrants.Count, 3,
                "WAVE-002: successive spawns must not clump on one side");
        }

        // B1 — ENM-001
        [Test]
        public void Enm001_ChaseStep_MovesTowardTarget()
        {
            var start = new Float3(0f, 0.5f, 0f);
            var target = new Float3(10f, 0.5f, 0f);

            var next = ChaseLogic.Step(start, target, speed: 4f, deltaTime: 0.5f);

            Assert.That(next.X, Is.EqualTo(2f).Within(1e-3f), "moved 4 m/s * 0.5s toward the target");
            Assert.That(next.Y, Is.EqualTo(start.Y).Within(1e-6f), "chase must not change height");
        }

        // B1 — ENM-001
        [Test]
        public void Enm001_ChaseStep_DoesNotOvershoot()
        {
            var start = new Float3(0f, 0.5f, 0f);
            var target = new Float3(1f, 0.5f, 0f);

            var next = ChaseLogic.Step(start, target, speed: 100f, deltaTime: 1f);

            Assert.That(next.X, Is.EqualTo(target.X).Within(1e-3f), "ENM-001: must stop at the target, not pass it");
        }

        // B8 — ENM-004
        [Test]
        public void Enm004_Separation_PushesApartWhenTooClose()
        {
            var self = new Float3(0f, 0f, 0f);
            var neighbours = new[] { new Float3(0.4f, 0f, 0f) };

            var push = SeparationLogic.Push(self, neighbours, 1, minDistance: 1.2f);

            Assert.Greater(push.Magnitude(), 0f, "ENM-004: overlapping drones must push apart");
            Assert.Less(push.X, 0f, "push must point away from the neighbour");
        }

        // B8 — ENM-004
        [Test]
        public void Enm004_Separation_IsZeroWhenFarEnough()
        {
            var self = Float3.Zero;
            var neighbours = new[] { new Float3(5f, 0f, 0f) };

            var push = SeparationLogic.Push(self, neighbours, 1, minDistance: 1.2f);

            Assert.That(push.Magnitude(), Is.LessThan(1e-6f), "no push when nothing is too close");
        }

        // B9 — ENM-004
        [Test]
        public void Enm004_Separation_IsWeakerThanChase()
        {
            var self = Float3.Zero;
            var neighbours = new[] { new Float3(0.01f, 0f, 0f) }; // maximum overlap
            const float chaseSpeed = 4f;
            const float ratio = 0.5f;

            var push = SeparationLogic.Push(self, neighbours, 1, minDistance: 1.2f);
            var velocity = SeparationLogic.SeparationVelocity(push, chaseSpeed, ratio);

            Assert.LessOrEqual(velocity.Magnitude(), chaseSpeed * ratio + 1e-4f,
                "ENM-004: separation must stay weaker than pursuit so the swarm still closes in");
        }
    }
}
