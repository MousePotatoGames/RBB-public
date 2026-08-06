using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// F11 setup: fills in the tesla's zap numbers, adds the zap controller to the
    /// player, and creates the greybox lightning line.
    ///
    /// Safe to re-run — every step is find-or-create.
    /// </summary>
    public static class F11TeslaSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string TeslaPath = "Assets/_Game/Configs/Weapons/Weapon_Tesla.asset";

        [MenuItem("RumbleBall/F11 — Build tesla zap")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("F11 setup: exit play mode first.");
                return;
            }

            var tesla = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(TeslaPath);
            if (tesla == null)
            {
                Debug.LogError($"F11 setup: {TeslaPath} not found — run the F09 setup first.");
                return;
            }

            // Spec tuning table (all TEMPORARY).
            tesla.attack = WeaponAttack.Zap;
            tesla.zapInterval = 0.9f;
            tesla.zapRange = 5f;
            tesla.zapMaxDamage = 12f;
            tesla.zapMinDamage = 4f;
            EditorUtility.SetDirty(tesla);

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var slots = FindInScene<WeaponSlots>(scene);
            var spawner = FindInScene<DroneSpawner>(scene);
            var session = FindInScene<GameSession>(scene);

            if (slots == null || spawner == null)
            {
                Debug.LogError($"F11 setup: slots={slots != null} droneSpawner={spawner != null} — run the earlier setups first.");
                return;
            }

            var controller = slots.GetComponent<ZapWeaponController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<ZapWeaponController>(slots.gameObject);
                Debug.Log("F11: added ZapWeaponController to " + slots.gameObject.name);
            }

            controller.Weapons = slots;
            controller.Enemies = spawner;
            controller.Session = session;
            EditorUtility.SetDirty(controller);

            ZapVisual visual = FindInScene<ZapVisual>(scene);
            if (visual == null)
            {
                var go = new GameObject("ZapVisual", typeof(LineRenderer), typeof(ZapVisual));
                Undo.RegisterCreatedObjectUndo(go, "F11 zap visual");
                visual = go.GetComponent<ZapVisual>();

                // Unlit so the greybox line is readable without lighting setup.
                var line = go.GetComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Color.cyan;
                line.endColor = Color.white;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;

                Debug.Log("F11: created ZapVisual");
            }

            visual.Controller = controller;
            EditorUtility.SetDirty(visual);

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
        [MenuItem("RumbleBall/F11 — Verify wiring")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            var controller = FindInScene<ZapWeaponController>(scene);
            var visual = FindInScene<ZapVisual>(scene);
            var tesla = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(TeslaPath);

            if (controller == null || visual == null || tesla == null)
            {
                Debug.LogError($"F11 verify: controller={controller != null} visual={visual != null} tesla={tesla != null}");
                return;
            }

            Debug.Log(
                $"F11 wiring — controller on '{controller.gameObject.name}': " +
                $"weapons={controller.Weapons != null} enemies={controller.Enemies != null} " +
                $"session={controller.Session != null} | " +
                $"visual.controller={visual.Controller != null} flash={visual.FlashDuration} | " +
                $"tesla {tesla.attack} interval={tesla.zapInterval} range={tesla.zapRange} " +
                $"dmg={tesla.zapMaxDamage}→{tesla.zapMinDamage}");
        }
    }
}
