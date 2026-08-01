// FirstPlayable — Play Mode marker
//
// Writes Temp/firstplayable_playmode while the Editor is in Play Mode so the
// unity-guard hook can refuse script edits that would deadlock the session:
//
//   play mode running -> script edited -> Unity defers recompile
//     -> MCP refuses every command (isCompiling) -> agent cannot stop play mode
//     -> only a human pressing Stop breaks the cycle
//
// See Docs/Process/WORKFLOW_PITFALLS.md A1 — this happened twice, once costing
// 26 minutes before manual intervention.
//
// Temp/ is gitignored and cleared by Unity, so a crash leaves at worst a stale
// marker; the hook message explains how to clear it.

using System.IO;
using UnityEditor;

namespace Game.Editor
{
    [InitializeOnLoad]
    internal static class PlayModeMarker
    {
        private const string MarkerPath = "Temp/firstplayable_playmode";

        static PlayModeMarker()
        {
            // Recover from a crash that left the marker behind.
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Clear();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    Directory.CreateDirectory("Temp");
                    File.WriteAllText(MarkerPath, "1");
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    Clear();
                    break;
            }
        }

        private static void Clear()
        {
            if (File.Exists(MarkerPath))
                File.Delete(MarkerPath);
        }
    }
}
