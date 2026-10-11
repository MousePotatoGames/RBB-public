// FirstPlayable — Play Mode marker
//
// Writes Temp/firstplayable_playmode while the Editor is in Play Mode so the
// unity-guard hook can refuse script edits that would deadlock the MCP bridge:
//
//   play mode running -> script edited -> Unity defers recompile
//     -> MCP refuses every command (isCompiling) -> agent cannot stop play mode
//     -> only a human pressing Stop breaks the cycle
//
// Temp/ is gitignored and cleared by Unity, so a crash leaves at worst a stale
// marker (the hook message explains how to clear it).
//
// Install to: Assets/_Game/Scripts/Editor/PlayModeMarker.cs

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
            EditorApplication.update += Heal;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Write BEFORE play mode starts, not on EnteredPlayMode: an agent's
            // "enter play mode" call can return seconds before EnteredPlayMode
            // fires, and the guard would be blind for that window.
            if (state == PlayModeStateChange.ExitingEditMode)
                Write();
            // Covers both a normal exit and an aborted entry (e.g. compile errors).
            else if (state == PlayModeStateChange.EnteredEditMode)
                Clear();
        }

        // Starting a PlayMode test run removed the marker after it was written
        // (observed with Unity 6.5 + Test Framework 1.7; the deleter was not
        // identified). Re-create it whenever play mode is on and it is missing.
        private static void Heal()
        {
            if (EditorApplication.isPlaying && !File.Exists(MarkerPath))
                Write();
        }

        private static void Write()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(MarkerPath, "1");
        }

        private static void Clear()
        {
            if (File.Exists(MarkerPath))
                File.Delete(MarkerPath);
        }
    }
}
