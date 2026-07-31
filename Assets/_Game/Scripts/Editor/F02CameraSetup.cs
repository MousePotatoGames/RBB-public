using System.Linq;
using Game.Presentation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Editor
{
    /// <summary>
    /// F02: builds the Cinemachine quarter-view rig in FirstPlayable.unity.
    /// Idempotent — re-running updates the existing rig instead of duplicating.
    /// Tuning values here are the spec's TEMPORARY initials; the scene
    /// components are the tuning surface afterwards.
    /// </summary>
    public static class F02CameraSetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity";
        private const string RigName = "CM_GameplayCamera";

        private const float OrbitRadius = 11f;   // TEMPORARY
        private const float FixedPitch = 38f;    // TEMPORARY
        private const float ScreenY = 0.08f;     // TEMPORARY — 플레이어 하단 ~42% 프레이밍 (CM3: +y가 화면 아래 방향, 런타임 측정으로 확정)
        private const float LookGainX = 2f;      // TEMPORARY

        [MenuItem("FirstPlayable/F02 Camera Setup")]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // 1. Brain on Main Camera — LateUpdate (CAM-003)
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[F02] Main Camera not found");
                return;
            }

            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            }
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;

            // 2. Rig object
            var rigGo = GameObject.Find(RigName);
            if (rigGo == null)
            {
                rigGo = new GameObject(RigName);
            }

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[F02] Player not found");
                return;
            }

            var cam = rigGo.GetComponent<CinemachineCamera>();
            if (cam == null)
            {
                cam = rigGo.AddComponent<CinemachineCamera>();
            }
            cam.Target.TrackingTarget = player.transform;

            // 3. OrbitalFollow — yaw orbit, fixed pitch (CAM-001)
            var orbital = rigGo.GetComponent<CinemachineOrbitalFollow>();
            if (orbital == null)
            {
                orbital = rigGo.AddComponent<CinemachineOrbitalFollow>();
            }
            orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbital.Radius = OrbitRadius;
            orbital.HorizontalAxis.Range = new Vector2(-180f, 180f);
            orbital.HorizontalAxis.Wrap = true;
            orbital.VerticalAxis.Value = FixedPitch;
            orbital.VerticalAxis.Range = new Vector2(FixedPitch, FixedPitch); // 고정 피치 (B2)
            orbital.TrackerSettings.PositionDamping = new Vector3(0.5f, 0.5f, 0.5f);

            // 4. RotationComposer — lower-band framing (CAM-002)
            var composer = rigGo.GetComponent<CinemachineRotationComposer>();
            if (composer == null)
            {
                composer = rigGo.AddComponent<CinemachineRotationComposer>();
            }
            var composition = composer.Composition;
            composition.ScreenPosition = new Vector2(0f, ScreenY);
            composer.Composition = composition;
            composer.Damping = new Vector2(0.3f, 0.3f);

            // 5. Input — mouse Look drives horizontal axis only (B1, B2)
            var input = rigGo.GetComponent<CinemachineInputAxisController>();
            if (input == null)
            {
                input = rigGo.AddComponent<CinemachineInputAxisController>();
            }
            input.SynchronizeControllers();
            var lookRef = FindLookActionReference();
            foreach (var controller in input.Controllers)
            {
                bool isHorizontal = controller.Name.Contains("Orbit X") || controller.Name.Contains("Horizontal");
                controller.Enabled = isHorizontal;
                if (isHorizontal)
                {
                    controller.Input.InputAction = lookRef;
                    controller.Input.Gain = LookGainX;
                }
            }

            // 6. Cursor lock (B7)
            if (Object.FindAnyObjectByType<CursorLockController>() == null)
            {
                mainCam.gameObject.AddComponent<CursorLockController>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[F02] Camera rig configured and scene saved. Look action = " + (lookRef != null ? lookRef.name : "NULL"));
        }

        /// <summary>Logs the rig wiring for the verify workflow. Returns true when every check holds.</summary>
        public static bool Verify()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            bool ok = true;
            var rig = GameObject.Find(RigName);
            if (rig == null)
            {
                Debug.LogError("[F02:Verify] rig missing");
                return false;
            }

            var vcam = rig.GetComponent<CinemachineCamera>();
            var orbital = rig.GetComponent<CinemachineOrbitalFollow>();
            var inputCtrl = rig.GetComponent<CinemachineInputAxisController>();
            var composer = rig.GetComponent<CinemachineRotationComposer>();

            bool followOk = vcam != null && vcam.Target.TrackingTarget != null && vcam.Target.TrackingTarget.name == "Player";
            bool pitchFixed = orbital != null && Mathf.Approximately(orbital.VerticalAxis.Range.x, orbital.VerticalAxis.Range.y);
            Debug.Log("[F02:Verify] follow=" + followOk
                + " radius=" + (orbital != null ? orbital.Radius.ToString("0.#") : "-")
                + " pitch=" + (orbital != null ? orbital.VerticalAxis.Value.ToString("0.#") : "-")
                + " pitchFixed=" + pitchFixed
                + " composerY=" + (composer != null ? composer.Composition.ScreenPosition.y.ToString("0.###") : "-"));
            ok &= followOk && pitchFixed && composer != null;

            bool horizontalWired = false;
            bool verticalDisabled = true;
            if (inputCtrl != null)
            {
                foreach (var c in inputCtrl.Controllers)
                {
                    bool isHorizontal = c.Name.Contains("Orbit X") || c.Name.Contains("Horizontal");
                    Debug.Log("[F02:Verify] ctrl '" + c.Name + "' enabled=" + c.Enabled
                        + " action=" + (c.Input.InputAction != null ? c.Input.InputAction.name : "null")
                        + " gain=" + c.Input.Gain);
                    if (isHorizontal && c.Enabled && c.Input.InputAction != null)
                    {
                        horizontalWired = true;
                    }
                    if (!isHorizontal && c.Enabled)
                    {
                        verticalDisabled = false;
                    }
                }
            }
            ok &= horizontalWired && verticalDisabled;

            var mainCam = Camera.main;
            var brain = mainCam != null ? mainCam.GetComponent<CinemachineBrain>() : null;
            bool brainOk = brain != null && brain.UpdateMethod == CinemachineBrain.UpdateMethods.LateUpdate;
            bool cursorOk = mainCam != null && mainCam.GetComponent<CursorLockController>() != null;
            Debug.Log("[F02:Verify] brainLateUpdate=" + brainOk + " cursorLock=" + cursorOk);
            ok &= brainOk && cursorOk;

            Debug.Log("[F02:Verify] RESULT = " + (ok ? "PASS" : "FAIL"));
            return ok;
        }

        private static InputActionReference FindLookActionReference()
        {
            return AssetDatabase.LoadAllAssetsAtPath("Assets/InputSystem_Actions.inputactions")
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null
                                     && r.action.name == "Look"
                                     && r.action.actionMap != null
                                     && r.action.actionMap.name == "Player");
        }
    }
}
