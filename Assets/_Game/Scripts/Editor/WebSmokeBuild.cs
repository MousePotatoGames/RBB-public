using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Web smoke build (DEVELOPMENT_ORDER 인프라 체크포인트).
    /// Unity MCP는 BuildPlayer를 "사용자 상호작용" API로 막으므로 메뉴에서 사람이 누른다.
    /// 결과는 Temp/에 써서 외부 도구가 읽을 수 있게 한다 (TestRunAutomation과 같은 규약).
    /// </summary>
    public static class WebSmokeBuild
    {
        private const string OutputDir = "Build/WebGL";
        private const string ResultPath = "Temp/firstplayable_build_result.txt";

        [MenuItem("RumbleBall/Build WebGL (smoke)")]
        public static void Build()
        {
            if (File.Exists(ResultPath))
            {
                File.Delete(ResultPath);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Game/Scenes/Gameplay/FirstPlayable.unity" },
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            BuildSummary s = BuildPipeline.BuildPlayer(options).summary;

            File.WriteAllText(ResultPath,
                $"RESULT={s.result}\nERRORS={s.totalErrors}\nWARNINGS={s.totalWarnings}\n" +
                $"SIZE_MB={s.totalSize / 1048576.0:0.0}\nSECONDS={(int)s.totalTime.TotalSeconds}\n");

            Debug.Log($"[BUILD] {s.result} errors={s.totalErrors} size={s.totalSize / 1048576.0:0.0}MB");
        }
    }
}
