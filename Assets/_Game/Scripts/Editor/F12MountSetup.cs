using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// F12 setup: creates the axe and pet definitions, grows the capsule pool to five,
    /// and wires the weapon container plus the sweep contact controller.
    ///
    /// Safe to re-run — every step is find-or-create.
    /// </summary>
    public static class F12MountSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string WeaponDir = "Assets/_Game/Configs/Weapons";

        [MenuItem("RumbleBall/F12 — Build orbit and follow mounts")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("F12 setup: exit play mode first.");
                return;
            }

            WeaponDefinition axe = EnsureDefinition("Weapon_Axe", WeaponKind.Axe, def =>
            {
                def.displayName = "Orbit Axe";
                def.mount = WeaponMount.Orbit;
                def.attack = WeaponAttack.Contact;
                def.orbitRadius = 2.2f;
                def.orbitAngularSpeed = 180f;
                def.orbitHeight = 0.3f;
                def.sweepRadius = 0.7f;
                def.sweepInterval = 0.25f;
                def.sweepDamage = 9f;
                def.shape = PrimitiveType.Cube;
                def.scale = 0.35f;
            });

            WeaponDefinition pet = EnsureDefinition("Weapon_Pet", WeaponKind.Pet, def =>
            {
                def.displayName = "Follow Pet";
                def.mount = WeaponMount.Follow;
                def.attack = WeaponAttack.Projectile;
                def.followStandoff = 1.8f;
                def.followSpeed = 6f;
                def.travel = ProjectileTravel.Straight;
                def.fireInterval = 1.0f;
                def.shotsPerBurst = 1;
                def.spreadDegrees = 0f;
                def.projectileSpeed = 14f;
                def.projectileRange = 10f;
                def.pierce = 0;
                def.projectileDamage = 5f;
                def.projectileRadius = 0.15f;
                def.shape = PrimitiveType.Sphere;
                def.scale = 0.32f;
            });

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var slots = FindInScene<WeaponSlots>(scene);
            var spawner = FindInScene<DroneSpawner>(scene);
            var capsules = FindInScene<CapsuleSpawner>(scene);
            var session = FindInScene<GameSession>(scene);

            if (slots == null || spawner == null || capsules == null)
            {
                Debug.LogError($"F12 setup: slots={slots != null} drones={spawner != null} capsules={capsules != null} — run the earlier setups first.");
                return;
            }

            WeaponContainer container = FindInScene<WeaponContainer>(scene);
            if (container == null)
            {
                var go = new GameObject("WeaponContainer", typeof(WeaponContainer));
                Undo.RegisterCreatedObjectUndo(go, "F12 weapon container");
                container = go.GetComponent<WeaponContainer>();
                Debug.Log("F12: created WeaponContainer");
            }

            container.Session = session;
            EditorUtility.SetDirty(container);

            slots.Container = container;
            EditorUtility.SetDirty(slots);

            var sweep = slots.GetComponent<SweepContactController>();
            if (sweep == null)
            {
                sweep = Undo.AddComponent<SweepContactController>(slots.gameObject);
                Debug.Log("F12: added SweepContactController to " + slots.gameObject.name);
            }

            sweep.Weapons = slots;
            sweep.Enemies = spawner;
            sweep.Session = session;
            EditorUtility.SetDirty(sweep);

            // WPN-005: five kinds against three slots is what makes the draw vary.
            WeaponDefinition spike = Load("Weapon_Spike");
            WeaponDefinition cannon = Load("Weapon_Cannon");
            WeaponDefinition tesla = Load("Weapon_Tesla");
            capsules.Pool = new List<WeaponDefinition> { spike, cannon, tesla, axe, pet };
            EditorUtility.SetDirty(capsules);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Verify();
        }

        private static WeaponDefinition Load(string fileName) =>
            AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{WeaponDir}/{fileName}.asset");

        private static WeaponDefinition EnsureDefinition(string fileName, WeaponKind kind, System.Action<WeaponDefinition> fill)
        {
            string path = $"{WeaponDir}/{fileName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            bool created = false;

            if (def == null)
            {
                def = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(def, path);
                created = true;
            }

            def.kind = kind;
            fill(def);
            EditorUtility.SetDirty(def);

            if (created)
            {
                Debug.Log($"F12: created {path}");
            }

            return def;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>Re-reads what Build() assigned — a recompile can null these out silently.</summary>
        [MenuItem("RumbleBall/F12 — Verify wiring")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            var slots = FindInScene<WeaponSlots>(scene);
            var sweep = FindInScene<SweepContactController>(scene);
            var container = FindInScene<WeaponContainer>(scene);
            var capsules = FindInScene<CapsuleSpawner>(scene);

            if (slots == null || sweep == null || container == null || capsules == null)
            {
                Debug.LogError($"F12 verify: slots={slots != null} sweep={sweep != null} container={container != null} capsules={capsules != null}");
                return;
            }

            var names = new List<string>();
            foreach (WeaponDefinition d in capsules.Pool)
            {
                names.Add(d != null ? $"{d.kind}({d.mount}/{d.attack})" : "(null)");
            }

            Debug.Log(
                $"F12 wiring — slots.container={slots.Container != null} | " +
                $"sweep: weapons={sweep.Weapons != null} enemies={sweep.Enemies != null} session={sweep.Session != null} | " +
                $"container.session={container.Session != null} | " +
                $"pool[{capsules.Pool.Count}] = {string.Join(", ", names)}");
        }
    }
}
