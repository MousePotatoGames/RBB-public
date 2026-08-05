using Game.Core;
using Game.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// F10 setup: fills in the cannon's projectile numbers, adds the projectile pool
    /// to the scene, and wires the firing controller onto the player.
    ///
    /// Safe to re-run — every step is find-or-create.
    /// </summary>
    public static class F10ProjectileSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string CannonPath = "Assets/_Game/Configs/Weapons/Weapon_Cannon.asset";

        [MenuItem("RumbleBall/F10 — Build projectile weapon")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("F10 setup: exit play mode first.");
                return;
            }

            var cannon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(CannonPath);
            if (cannon == null)
            {
                Debug.LogError($"F10 setup: {CannonPath} not found — run the F09 setup first.");
                return;
            }

            // Spec tuning table (all TEMPORARY).
            cannon.attack = WeaponAttack.Projectile;
            cannon.travel = ProjectileTravel.Straight;
            cannon.fireInterval = 0.7f;
            cannon.shotsPerBurst = 1;
            cannon.spreadDegrees = 0f;
            cannon.projectileSpeed = 18f;
            cannon.projectileRange = 14f;
            cannon.pierce = 0;
            cannon.projectileDamage = 6f;
            cannon.projectileRadius = 0.15f;
            EditorUtility.SetDirty(cannon);

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var slots = FindInScene<WeaponSlots>(scene);
            var session = FindInScene<GameSession>(scene);

            if (slots == null)
            {
                Debug.LogError("F10 setup: WeaponSlots not found — run the earlier setups first.");
                return;
            }

            ProjectilePool pool = FindInScene<ProjectilePool>(scene);
            if (pool == null)
            {
                var go = new GameObject("ProjectilePool", typeof(ProjectilePool));
                Undo.RegisterCreatedObjectUndo(go, "F10 projectile pool");
                pool = go.GetComponent<ProjectilePool>();
                Debug.Log("F10: created ProjectilePool");
            }

            pool.LiveCap = 32;
            pool.Session = session;
            EditorUtility.SetDirty(pool);

            var controller = slots.GetComponent<ProjectileWeaponController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<ProjectileWeaponController>(slots.gameObject);
                Debug.Log("F10: added ProjectileWeaponController to " + slots.gameObject.name);
            }

            controller.Weapons = slots;
            controller.Pool = pool;
            controller.Session = session;
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Verify();
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
        [MenuItem("RumbleBall/F10 — Verify wiring")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            var controller = FindInScene<ProjectileWeaponController>(scene);
            var pool = FindInScene<ProjectilePool>(scene);
            var cannon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(CannonPath);

            if (controller == null || pool == null || cannon == null)
            {
                Debug.LogError($"F10 verify: controller={controller != null} pool={pool != null} cannon={cannon != null}");
                return;
            }

            Debug.Log(
                $"F10 wiring — controller on '{controller.gameObject.name}': " +
                $"weapons={controller.Weapons != null} pool={controller.Pool != null} " +
                $"session={controller.Session != null} | " +
                $"pool.liveCap={pool.LiveCap} pool.session={pool.Session != null} | " +
                $"cannon {cannon.attack}/{cannon.travel} interval={cannon.fireInterval} shots={cannon.shotsPerBurst} " +
                $"spread={cannon.spreadDegrees} speed={cannon.projectileSpeed} range={cannon.projectileRange} " +
                $"dmg={cannon.projectileDamage}");
        }
    }
}
