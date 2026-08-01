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
    /// F07 scene setup (LOSE-001). Builds the GameSession director and the result
    /// screen in FirstPlayable.unity and wires every reference, so scene changes
    /// go through the Editor API instead of hand-editing YAML.
    /// Re-running it is safe — existing objects are reused.
    /// </summary>
    public static class F07SessionSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string ConfigPath = "Assets/_Game/Configs/SessionConfig.asset";

        [MenuItem("RumbleBall/F07 — Build session & result screen")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("F07 setup: exit play mode first.");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            SessionConfig config = EnsureConfig();
            GameSession session = EnsureSession(scene, config);
            EnsureResultScreen(scene, config, session);
            WireExisting(session);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Verify();
        }

        private static SessionConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<SessionConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<SessionConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
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

        private static GameSession EnsureSession(Scene scene, SessionConfig config)
        {
            GameSession session = FindInScene<GameSession>(scene);
            if (session == null)
            {
                var go = new GameObject("GameSession");
                session = go.AddComponent<GameSession>();
            }

            session.Config = config;
            session.PlayerHealth = FindInScene<PlayerHealth>(scene);
            session.DamageDealer = FindInScene<PlayerDamageDealer>(scene);
            EditorUtility.SetDirty(session);
            return session;
        }

        private static void EnsureResultScreen(Scene scene, SessionConfig config, GameSession session)
        {
            // uGUI needs an EventSystem for button clicks.
            if (FindInScene<EventSystem>(scene) == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            ResultScreen screen = FindInScene<ResultScreen>(scene);
            GameObject canvasGo;

            if (screen != null)
            {
                canvasGo = screen.gameObject;
            }
            else
            {
                canvasGo = new GameObject("ResultCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                screen = canvasGo.AddComponent<ResultScreen>();
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = FindChild(canvasGo.transform, "Panel");
            if (panel == null)
            {
                panel = NewUIObject("Panel", canvasGo.transform);
                var image = panel.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.72f);
                Stretch(panel.GetComponent<RectTransform>());
            }

            TextMeshProUGUI outcome = EnsureLabel(panel.transform, "Outcome", 120f,
                new Vector2(0f, 160f), new Vector2(1200f, 200f));
            TextMeshProUGUI stats = EnsureLabel(panel.transform, "Stats", 56f,
                new Vector2(0f, -20f), new Vector2(1200f, 220f));

            Button retry = EnsureRetryButton(panel.transform);

            screen.Config = config;
            screen.Session = session;
            screen.Panel = panel;
            screen.OutcomeLabel = outcome;
            screen.StatsLabel = stats;
            screen.RetryButton = retry;

            panel.SetActive(false); // B13
            EditorUtility.SetDirty(screen);
        }

        private static Button EnsureRetryButton(Transform parent)
        {
            GameObject go = FindChild(parent, "RetryButton");
            if (go == null)
            {
                go = NewUIObject("RetryButton", parent);
                var image = go.AddComponent<Image>();
                image.color = new Color(0.85f, 0.15f, 0.55f, 1f);
                go.AddComponent<Button>();

                GameObject labelGo = NewUIObject("Label", go.transform);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = "RETRY  (R / Space)";
                label.fontSize = 40f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                Stretch(labelGo.GetComponent<RectTransform>());
            }

            var rect = go.GetComponent<RectTransform>();
            Centre(rect, new Vector2(0f, -220f), new Vector2(520f, 110f));

            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        private static TextMeshProUGUI EnsureLabel(Transform parent, string name, float size, Vector2 position, Vector2 sizeDelta)
        {
            GameObject go = FindChild(parent, name);
            if (go == null)
            {
                go = NewUIObject(name, parent);
                go.AddComponent<TextMeshProUGUI>();
            }

            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.text = name;
            Centre(go.GetComponent<RectTransform>(), position, sizeDelta);
            return text;
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

        /// <summary>B6/B7: existing components need to know about the session.</summary>
        private static void WireExisting(GameSession session)
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            var hitStop = FindInScene<HitStopController>(scene);
            if (hitStop != null)
            {
                hitStop.Session = session;
                EditorUtility.SetDirty(hitStop);
            }

            var cursor = FindInScene<CursorLockController>(scene);
            if (cursor != null)
            {
                cursor.Session = session;
                EditorUtility.SetDirty(cursor);
            }
        }

        [MenuItem("RumbleBall/F07 — Verify wiring")]
        public static void Verify()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            var session = FindInScene<GameSession>(scene);
            var screen = FindInScene<ResultScreen>(scene);
            var hitStop = FindInScene<HitStopController>(scene);
            var cursor = FindInScene<CursorLockController>(scene);

            if (session == null || screen == null)
            {
                Debug.LogError("F07 verify: GameSession or ResultScreen missing");
                return;
            }

            Debug.Log(
                $"F07 wiring — session.config={session.Config != null} health={session.PlayerHealth != null} " +
                $"dealer={session.DamageDealer != null} | screen.config={screen.Config != null} " +
                $"session={screen.Session != null} panel={screen.Panel != null} outcome={screen.OutcomeLabel != null} " +
                $"stats={screen.StatsLabel != null} retry={screen.RetryButton != null} " +
                $"panelHidden={screen.Panel != null && !screen.Panel.activeSelf} | " +
                $"hitStop.session={(hitStop != null ? (hitStop.Session != null).ToString() : "n/a")} " +
                $"cursor.session={(cursor != null ? (cursor.Session != null).ToString() : "n/a")} | " +
                $"eventSystem={FindInScene<EventSystem>(scene) != null}");
        }
    }
}
