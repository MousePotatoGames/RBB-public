using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// WPN-001 / WPN-001a / WPN-005 coverage (Docs/Features/F08-weapon-attachment.md).
    /// </summary>
    public sealed class WeaponAttachmentLogicTests
    {
        private const float MinSeparation = 50f;

        private static float AngleBetween(Float3 a, Float3 b)
        {
            float dot = Float3.Dot(a.Normalized(), b.Normalized());
            if (dot > 1f) dot = 1f;
            if (dot < -1f) dot = -1f;
            return (float)(Math.Acos(dot) * 180.0 / Math.PI);
        }

        // B1 — the contact point is used as-is, not snapped
        [Test]
        public void Wpn001_FirstWeapon_UsesContactDirectionExactly()
        {
            var contact = new Float3(0.3f, 0.7f, -0.2f);

            Float3 result = AttachmentLogic.Resolve(contact, new Float3[3], 0, MinSeparation);

            Assert.That(AngleBetween(result, contact), Is.LessThan(1e-3f),
                "WPN-001: with nothing in the way the weapon must sit exactly where it was hit");
        }

        // B2
        [Test]
        public void Wpn001_Result_IsAlwaysUnitLength()
        {
            var taken = new[] { new Float3(0f, 1f, 0f) };

            Float3 far = AttachmentLogic.Resolve(new Float3(0f, -1f, 0f), taken, 1, MinSeparation);
            Float3 near = AttachmentLogic.Resolve(new Float3(0.05f, 1f, 0f), taken, 1, MinSeparation);

            Assert.That(far.Magnitude(), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(near.Magnitude(), Is.EqualTo(1f).Within(1e-4f));
        }

        // B3 — this is the whole point of the 2026-08-05 rule change.
        // With the old 12-slot snapping both contacts would land on one slot.
        [Test]
        public void Wpn001_TinyAngleDifference_ProducesDifferentPosition()
        {
            var a = new Float3(0f, 1f, 0f);
            var b = new Float3(0.02f, 1f, 0f); // ~1 degree apart

            Float3 ra = AttachmentLogic.Resolve(a, new Float3[3], 0, MinSeparation);
            Float3 rb = AttachmentLogic.Resolve(b, new Float3[3], 0, MinSeparation);

            Assert.Greater(AngleBetween(ra, rb), 0.1f,
                "WPN-001: attachment must be continuous — a small change in contact angle must move the weapon");
        }

        // B3
        [Test]
        public void Wpn001_OppositeContact_StaysOpposite()
        {
            Float3 up = AttachmentLogic.Resolve(new Float3(0f, 1f, 0f), new Float3[3], 0, MinSeparation);
            Float3 down = AttachmentLogic.Resolve(new Float3(0f, -1f, 0f), new Float3[3], 0, MinSeparation);

            Assert.That(AngleBetween(up, down), Is.EqualTo(180f).Within(0.1f));
        }

        // B4
        [Test]
        public void Wpn001a_TooCloseToExisting_IsPushedAway()
        {
            var existing = new Float3(0f, 1f, 0f);
            var taken = new[] { existing };
            var contact = new Float3(0.1f, 1f, 0f); // only a few degrees away

            Float3 result = AttachmentLogic.Resolve(contact, taken, 1, MinSeparation);

            Assert.That(AngleBetween(result, existing), Is.EqualTo(MinSeparation).Within(0.5f),
                "WPN-001a: a crowded contact must be pushed out to exactly the minimum gap");
        }

        // B4 — the push must go away from the existing weapon, keeping the player's intent
        [Test]
        public void Wpn001a_Push_KeepsTheContactSide()
        {
            var existing = new Float3(0f, 1f, 0f);
            var taken = new[] { existing };

            Float3 pushedX = AttachmentLogic.Resolve(new Float3(0.1f, 1f, 0f), taken, 1, MinSeparation);
            Float3 pushedZ = AttachmentLogic.Resolve(new Float3(0f, 1f, 0.1f), taken, 1, MinSeparation);

            Assert.Greater(pushedX.X, 0.5f, "a contact on the +X side must be pushed towards +X");
            Assert.Greater(pushedZ.Z, 0.5f, "a contact on the +Z side must be pushed towards +Z");
        }

        // B4 — a contact that is already clear must not be moved at all
        [Test]
        public void Wpn001a_FarFromExisting_IsNotMoved()
        {
            var taken = new[] { new Float3(0f, 1f, 0f) };
            var contact = new Float3(0f, -1f, 0f);

            Float3 result = AttachmentLogic.Resolve(contact, taken, 1, MinSeparation);

            Assert.That(AngleBetween(result, contact), Is.LessThan(1e-3f),
                "WPN-001a: separation only applies when the gap is actually too small");
        }

        // B6 — the silhouette guarantee the rule exists for
        [Test]
        public void Wpn001a_AllPairs_KeepMinimumSeparation()
        {
            var taken = new Float3[3];
            int count = 0;

            // Three weapons deliberately aimed at almost the same spot.
            var contacts = new[]
            {
                new Float3(0f, 1f, 0f),
                new Float3(0.05f, 1f, 0f),
                new Float3(0.05f, 1f, 0.05f),
            };

            foreach (Float3 contact in contacts)
            {
                taken[count] = AttachmentLogic.Resolve(contact, taken, count, MinSeparation);
                count++;
            }

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    Assert.GreaterOrEqual(AngleBetween(taken[i], taken[j]), MinSeparation - 0.5f,
                        $"WPN-001a: weapons {i} and {j} overlap — the silhouette guarantee is broken");
                }
            }
        }

        // B2, B4
        [Test]
        public void Wpn001a_PushedResult_IsStillUnitLength()
        {
            var taken = new[] { new Float3(0f, 1f, 0f), new Float3(1f, 0f, 0f) };

            Float3 result = AttachmentLogic.Resolve(new Float3(0.1f, 1f, 0.1f), taken, 2, MinSeparation);

            Assert.That(result.Magnitude(), Is.EqualTo(1f).Within(1e-4f));
        }

        // B1 — a degenerate contact must still produce a usable direction
        [Test]
        public void Wpn001_ZeroContactDirection_FallsBackInsteadOfFailing()
        {
            Float3 result = AttachmentLogic.Resolve(Float3.Zero, new Float3[3], 0, MinSeparation);

            Assert.That(result.Magnitude(), Is.EqualTo(1f).Within(1e-4f),
                "a capsule sitting exactly at the ball's centre must not become unclaimable");
        }

        // B4 — an exactly coincident contact has no "away" direction; it must still resolve
        [Test]
        public void Wpn001a_ExactlyCoincidentContact_IsStillSeparated()
        {
            var existing = new Float3(0f, 1f, 0f);
            var taken = new[] { existing };

            Float3 result = AttachmentLogic.Resolve(existing, taken, 1, MinSeparation);

            Assert.That(AngleBetween(result, existing), Is.EqualTo(MinSeparation).Within(0.5f));
            Assert.That(result.Magnitude(), Is.EqualTo(1f).Within(1e-4f));
        }

        // WPN-005 (2026-08-05 개정) — pool larger than slots
        [Test]
        public void Wpn005_DrawIndices_PicksSlotCountWithoutRepeats()
        {
            int[] picked = WeaponDrawLogic.DrawIndices(poolSize: 5, pick: 3, seed: 4);

            Assert.AreEqual(3, picked.Length);
            CollectionAssert.AllItemsAreUnique(picked);
            foreach (int i in picked)
            {
                Assert.That(i, Is.InRange(0, 4));
            }
        }

        // WPN-005 — this is what makes runs differ; with pool == slots it never could
        [Test]
        public void Wpn005_DrawIndices_SetVariesBetweenRuns()
        {
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 60; seed++)
            {
                int[] picked = WeaponDrawLogic.DrawIndices(5, 3, seed);
                System.Array.Sort(picked);              // compare the set, not the order
                seen.Add(string.Join(",", picked));
            }

            Assert.Greater(seen.Count, 1,
                "WPN-005: with more weapon kinds than slots the *set* must differ between runs (HYP-006)");
        }

        // WPN-005 Exception — pool at or below slot count means everything appears
        [Test]
        public void Wpn005_DrawIndices_PoolAtOrBelowSlots_ReturnsAll()
        {
            int[] picked = WeaponDrawLogic.DrawIndices(poolSize: 3, pick: 3, seed: 9);

            Assert.AreEqual(3, picked.Length);
            CollectionAssert.AllItemsAreUnique(picked);
        }

        [Test]
        public void Wpn005_DrawIndices_EmptyPool_ReturnsEmpty()
        {
            Assert.AreEqual(0, WeaponDrawLogic.DrawIndices(0, 3, 1).Length);
        }
    }
}
