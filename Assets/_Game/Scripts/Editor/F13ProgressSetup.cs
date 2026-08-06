using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// F13 stage 1 setup: progress config, the three passive assets, the orb pool,
    /// the level-up director and its card screen.
    ///
    /// Safe to re-run — every step is find-or-create.
    /// </summary>
    public static class F13ProgressSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string ConfigDir = "Assets/_Game/Configs";
        private const string PassiveDir = "Assets/_Game/Configs/Passives";

        [MenuItem("RumbleBall/F13 — Build experience and level-ups")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("F13 setup: exit play mode first.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(PassiveDir))
            {
                AssetDatabase.CreateFolder(ConfigDir, "Passives");
            }

            ProgressConfig progressConfig = EnsureProgressConfig();

            PassiveDefinition armor = EnsurePassive("Passive_ArmorPlating", PassiveKind.ArmorPlating, def =>
            {
                def.displayName = "ARMOR PLATING";
                def.maxHealthPerStage = 20f;
                def.healPerStage = 20f;
            });

            PassiveDefinition core = EnsurePassive("Passive_DenseCore", PassiveKind.DenseCore, def =>
            {
                def.displayName = "DENSE CORE";
                def.speedMultiplierPerStage = 1.08f;
                def.damageMultiplierPerStage = 1.15f;
            });

            PassiveDefinition magnet = EnsurePassive("Passive_MagnetField", PassiveKind.MagnetField, def =>
            {
                def.displayName = "MAGNET FIELD";
                def.magnetMultiplierPerStage = 1.6f;
                def.experienceMultiplierPerStage = 1.25f;
            });

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var session = FindInScene<GameSession>(scene);
            var dealer = FindInScene<PlayerDamageDealer>(scene);

            if (session == null || dealer == null)
            {
                Debug.LogError($"F13 setup: session={session != null} dealer={dealer != null} — run the earlier setups first.");
                return;
            }

            // PlayerProgress rides the player, next to the dealer that feeds it.
            var progress = dealer.GetComponent<PlayerProgress>();
            if (progress == null)
            {
                progress = Undo.AddComponent<PlayerProgress>(dealer.gameObject);
                Debug.Log("F13: added PlayerProgress to " + dealer.gameObject.name);
            }

            ExperienceOrbPool orbs = FindInScene<ExperienceOrbPool>(scene);
            if (orbs == null)
            {
                var go = new GameObject("ExperienceOrbPool", typeof(ExperienceOrbPool));
                Undo.RegisterCreatedObjectUndo(go, "F13 orb pool");
                orbs = go.GetComponent<ExperienceOrbPool>();
                Debug.Log("F13: created ExperienceOrbPool");
            }

            orbs.Config = progressConfig;
            orbs.KillSource = dealer;
            orbs.Player = dealer.transform;
            orbs.Session = session;
            orbs.Progress = progress;
            EditorUtility.SetDirty(orbs);

            progress.Config = progressConfig;
            progress.Orbs = orbs;
            progress.PassiveAssets = new List<PassiveDefinition> { armor, core, magnet };
            progress.Health = dealer.GetComponent<PlayerHealth>();
            EditorUtility.SetDirty(progress);

            // PAS-001~003 (B21~B23): the four consumers that read effective values.
            var motor = dealer.GetComponent<BallMotor>();
            if (motor != null)
            {
                motor.Progress = progress;
                EditorUtility.SetDirty(motor);
            }

            var playerHealth = dealer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Progress = progress;
                EditorUtility.SetDirty(playerHealth);
            }

            dealer.Progress = progress;
            EditorUtility.SetDirty(dealer);

            LevelUpDirector director = FindInScene<LevelUpDirector>(scene);
            if (director == null)
            {
                var go = new GameObject("LevelUpDirector", typeof(LevelUpDirector));
                Undo.RegisterCreatedObjectUndo(go, "F13 level-up director");
                director = go.GetComponent<LevelUpDirector>();
                Debug.Log("F13: created LevelUpDirector");
            }

            director.Config = progressConfig;
            director.Progress = progress;
            director.Session = session;
            director.Passives = new List<PassiveDefinition> { armor, core, magnet };
            EditorUtility.SetDirty(director);

            // Both of these own Time.timeScale / the cursor and have to stand down
            // while the cards are up (LVL-001 B10).
            var hitStop = FindInScene<HitStopController>(scene);
            if (hitStop != null)
            {
                hitStop.LevelUp = director;
                EditorUtility.SetDirty(hitStop);
            }

            var cursor = FindInScene<CursorLockController>(scene);
            if (cursor != null)
            {
                cursor.LevelUp = director;
                EditorUtility.SetDirty(cursor);
            }

            EnsureLevelUpScreen(scene, director);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Verify();
        }

        private static ProgressConfig EnsureProgressConfig()
        {
            const string path = ConfigDir + "/ProgressConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<ProgressConfig>(path);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ProgressConfig>();
                AssetDatabase.CreateAsset(config, path);
                Debug.Log("F13: created " + path);
            }

            // Defaults only — an existing asset keeps whatever the playtest moved it to.
            return config;
        }

        private static PassiveDefinition EnsurePassive(string fileName, PassiveKind kind, System.Action<PassiveDefinition> fill)
        {
            string path = $"{PassiveDir}/{fileName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<PassiveDefinition>(path);
            bool created = false;

            if (def == null)
            {
                def = ScriptableObject.CreateInstance<PassiveDefinition>();
                AssetDatabase.CreateAsset(def, path);
                created = true;
            }

            def.kind = kind;
            fill(def);
            EditorUtility.SetDirty(def);

            if (created)
            {
                Debug.Log($"F13: created {path}");
            }

            return def;
        }

        // ---- card screen ---------------------------------------------------------

        private static void EnsureLevelUpScreen(Scene scene, LevelUpDirector director)
        {
            if (FindInScene<EventSystem>(scene) == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            LevelUpScreen screen = FindInScene<LevelUpScreen>(scene);
            GameObject canvasGo;

            if (screen != null)
            {
                canvasGo = screen.gameObject;
            }
            else
            {
                canvasGo = new GameObject("LevelUpCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                screen = canvasGo.AddComponent<LevelUpScreen>();
                Debug.Log("F13: created LevelUpCanvas");
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // under the result screen (100) — F07 owns the end

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = FindChild(canvasGo.transform, "Panel");
            if (panel == null)
            {
                panel = NewUIObject("Panel", canvasGo.transform);
                var image = panel.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.78f);
                Stretch(panel.GetComponent<RectTransform>());
            }

            TextMeshProUGUI title = EnsureLabel(panel.transform, "Title", 90f,
                new Vector2(0f, 340f), new Vector2(1400f, 160f), "LEVEL UP");

            int cards = PassiveLogic.KindCount;
            var buttons = new Button[cards];
            var labels = new TextMeshProUGUI[cards];

            const float cardWidth = 420f;
            const float gap = 40f;
            float span = cards * cardWidth + (cards - 1) * gap;
            float left = -span * 0.5f + cardWidth * 0.5f;

            for (int i = 0; i < cards; i++)
            {
                float x = left + i * (cardWidth + gap);
                EnsureCard(panel.transform, i, new Vector2(x, -20f), new Vector2(cardWidth, 460f),
                    out buttons[i], out labels[i]);
            }

            screen.Director = director;
            screen.Panel = panel;
            screen.TitleLabel = title;
            screen.CardButtons = buttons;
            screen.CardLabels = labels;

            panel.SetActive(false);
            EditorUtility.SetDirty(screen);
        }

        private static void EnsureCard(Transform parent, int index, Vector2 position, Vector2 size,
            out Button button, out TextMeshProUGUI label)
        {
            string name = $"Card{index + 1}";
            GameObject go = FindChild(parent, name);

            if (go == null)
            {
                go = NewUIObject(name, parent);
                var image = go.AddComponent<Image>();
                image.color = new Color(0.12f, 0.14f, 0.20f, 0.96f);
                go.AddComponent<Button>();

                GameObject labelGo = NewUIObject("Label", go.transform);
                var text = labelGo.AddComponent<TextMeshProUGUI>();
                text.fontSize = 38f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                Stretch(labelGo.GetComponent<RectTransform>());
            }

            Centre(go.GetComponent<RectTransform>(), position, size);

            button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            label = go.GetComponentInChildren<TextMeshProUGUI>();
        }

        private static TextMeshProUGUI EnsureLabel(Transform parent, string name, float size,
            Vector2 position, Vector2 sizeDelta, string text)
        {
            GameObject go = FindChild(parent, name);
            if (go == null)
            {
                go = NewUIObject(name, parent);
                go.AddComponent<TextMeshProUGUI>();
            }

            var label = go.GetComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.text = text;
            Centre(go.GetComponent<RectTransform>(), position, sizeDelta);
            return label;
        }

        private static GameObject NewUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            return t != null ? t.gameObject : null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Centre(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
        [MenuItem("RumbleBall/F13 — Verify wiring")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            var progress = FindInScene<PlayerProgress>(scene);
            var orbs = FindInScene<ExperienceOrbPool>(scene);
            var director = FindInScene<LevelUpDirector>(scene);
            var screen = FindInScene<LevelUpScreen>(scene);
            var hitStop = FindInScene<HitStopController>(scene);
            var cursor = FindInScene<CursorLockController>(scene);

            if (progress == null || orbs == null || director == null || screen == null)
            {
                Debug.LogError($"F13 verify: progress={progress != null} orbs={orbs != null} " +
                               $"director={director != null} screen={screen != null}");
                return;
            }

            int passiveCount = director.Passives != null ? director.Passives.Count : 0;
            var kinds = new List<string>();
            for (int i = 0; i < passiveCount; i++)
            {
                PassiveDefinition d = director.Passives[i];
                kinds.Add(d != null ? $"{d.kind}({d.displayName})" : "(null)");
            }

            var motor = FindInScene<BallMotor>(scene);
            var playerHealth = FindInScene<PlayerHealth>(scene);
            var dealer = FindInScene<PlayerDamageDealer>(scene);

            Debug.Log(
                $"F13 effects — motor={(motor != null ? (motor.Progress != null).ToString() : "n/a")} " +
                $"health={(playerHealth != null ? (playerHealth.Progress != null).ToString() : "n/a")} " +
                $"dealer={(dealer != null ? (dealer.Progress != null).ToString() : "n/a")} " +
                $"orbs={orbs.Progress != null} " +
                $"progressPassives={(progress.PassiveAssets != null ? progress.PassiveAssets.Count : 0)} " +
                $"healTarget={progress.Health != null}");

            Debug.Log(
                $"F13 wiring — progress: config={progress.Config != null} orbs={progress.Orbs != null} | " +
                $"orbs: config={orbs.Config != null} kills={orbs.KillSource != null} player={orbs.Player != null} session={orbs.Session != null} | " +
                $"director: progress={director.Progress != null} session={director.Session != null} passives[{passiveCount}]={string.Join(", ", kinds)} | " +
                $"screen: director={screen.Director != null} panel={screen.Panel != null} cards={screen.CardButtons.Length}/{screen.CardLabels.Length} | " +
                $"handoff: hitStop={(hitStop != null ? (hitStop.LevelUp != null).ToString() : "n/a")} " +
                $"cursor={(cursor != null ? (cursor.LevelUp != null).ToString() : "n/a")}");
        }
    }
}
