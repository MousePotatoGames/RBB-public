using Game.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// F08 scene setup (WPN-001 계열). Adds WeaponSlots to the player and a
    /// CapsuleSpawner to the scene, then wires and verifies every reference.
    /// Safe to re-run.
    /// </summary>
    public static class F08WeaponSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string ConfigPath = "Assets/_Game/Configs/WeaponConfig.asset";

        [MenuItem("RumbleBall/F08 — Build weapons & capsules")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("F08 setup: exit play mode first.");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            WeaponConfig config = EnsureConfig();

            // WeaponSlots lives on the ball itself — attachments are its children.
            var motor = FindInScene<BallMotor>(scene);
            if (motor == null)
            {
                Debug.LogError("F08 setup: BallMotor (player) not found.");
                return;
            }

            var slots = motor.GetComponent<WeaponSlots>();
            if (slots == null)
            {
                slots = motor.gameObject.AddComponent<WeaponSlots>();
            }

            slots.Config = config;
            EditorUtility.SetDirty(slots);

            var spawner = FindInScene<CapsuleSpawner>(scene);
            if (spawner == null)
            {
                var go = new GameObject("CapsuleSpawner");
                spawner = go.AddComponent<CapsuleSpawner>();
            }

            spawner.Config = config;
            spawner.Player = slots;
            spawner.Session = FindInScene<GameSession>(scene);
            EditorUtility.SetDirty(spawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Verify();
        }

        private static WeaponConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<WeaponConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
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
        [MenuItem("RumbleBall/F08 — Verify wiring")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            var slots = FindInScene<WeaponSlots>(scene);
            var spawner = FindInScene<CapsuleSpawner>(scene);

            if (slots == null || spawner == null)
            {
                Debug.LogError($"F08 verify: slots={slots != null} spawner={spawner != null}");
                return;
            }

            Debug.Log(
                $"F08 wiring — slots.config={slots.Config != null} onPlayer={slots.GetComponent<BallMotor>() != null} | " +
                $"spawner.config={spawner.Config != null} player={spawner.Player != null} session={spawner.Session != null} | " +
                $"spawnTimes=[{string.Join(", ", spawner.Config != null ? spawner.Config.spawnTimes : new float[0])}]");
        }
    }
}
