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
    /// F09 setup: creates the weapon definition assets and points the spawner's
    /// pool at them, then verifies every reference.
    ///
    /// Only the spike is a working weapon in F09 — the cannon and tesla definitions
    /// exist so the draw has a pool, and their attack types are filled in by F10/F11.
    /// Safe to re-run.
    /// </summary>
    public static class F09WeaponSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string WeaponDir = "Assets/_Game/Configs/Weapons";

        [MenuItem("RumbleBall/F09 — Build weapon definitions")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("F09 setup: exit play mode first.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(WeaponDir))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Configs", "Weapons");
            }

            WeaponDefinition spike = EnsureDefinition("Weapon_Spike", WeaponKind.Spike, def =>
            {
                def.displayName = "Crusher Spike";
                def.mount = WeaponMount.Surface;
                def.attack = WeaponAttack.Contact;   // F09
                def.arcHalfAngleDegrees = 60f;
                def.bonusDamage = 8f;
                def.dashDamageMultiplier = 1.5f;
                def.dashKnockbackMultiplier = 1.4f;
                def.shape = PrimitiveType.Capsule;
                def.scale = 0.28f;
            });

            WeaponDefinition cannon = EnsureDefinition("Weapon_Cannon", WeaponKind.Cannon, def =>
            {
                def.displayName = "Revolver Cannon";
                def.mount = WeaponMount.Surface;
                def.attack = WeaponAttack.Projectile; // F10 fills in the behaviour
                def.shape = PrimitiveType.Cylinder;
                def.scale = 0.28f;
            });

            WeaponDefinition tesla = EnsureDefinition("Weapon_Tesla", WeaponKind.Tesla, def =>
            {
                def.displayName = "Tesla Ring";
                def.mount = WeaponMount.Surface;
                def.attack = WeaponAttack.Zap;        // F11 fills in the behaviour
                def.shape = PrimitiveType.Cube;
                def.scale = 0.28f;
            });

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var spawner = FindInScene<CapsuleSpawner>(scene);
            if (spawner == null)
            {
                Debug.LogError("F09 setup: CapsuleSpawner not found — run the F08 setup first.");
                return;
            }

            spawner.Pool = new List<WeaponDefinition> { spike, cannon, tesla };
            EditorUtility.SetDirty(spawner);

            // SPK-001: the dealer needs the slots to add weapon bonus damage.
            var dealer = FindInScene<PlayerDamageDealer>(scene);
            var slots = FindInScene<WeaponSlots>(scene);
            if (dealer != null && slots != null)
            {
                dealer.Weapons = slots;
                EditorUtility.SetDirty(dealer);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Verify();
        }

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
                Debug.Log($"F09: created {path}");
            }

            return def;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>Re-reads what Build() assigned — a recompile can null these out silently.</summary>
        [MenuItem("RumbleBall/F09 — Verify wiring")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            var spawner = FindInScene<CapsuleSpawner>(scene);
            var dealer = FindInScene<PlayerDamageDealer>(scene);

            if (spawner == null || dealer == null)
            {
                Debug.LogError($"F09 verify: spawner={spawner != null} dealer={dealer != null}");
                return;
            }

            var names = new List<string>();
            foreach (WeaponDefinition d in spawner.Pool)
            {
                names.Add(d != null ? $"{d.kind}/{d.mount}/{d.attack}" : "(null)");
            }

            Debug.Log($"F09 wiring — pool[{spawner.Pool.Count}] = {string.Join(", ", names)} | " +
                      $"dealer.weapons={dealer.Weapons != null} | maxWeapons={spawner.Config?.maxWeapons}");
        }
    }
}
