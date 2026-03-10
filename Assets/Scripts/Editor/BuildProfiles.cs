#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace PocoRender.Editor
{
    /// <summary>
    /// Provides menu-driven build profiles:
    ///
    ///   1. Standalone — full Unity UI, for independent development.
    ///      Output: Build/Standalone/PocoRenderStudio.exe (relative to Unity project)
    ///
    ///   2. Build for PocoStudio (Embedded) — EMBEDDED_MODE for Qt hosting.
    ///      Output: NewWork/poco_studio/prebuild/unity/PocoRenderStudio.exe
    ///      UI is stripped for embedding; Qt provides HOME, Unity provides Canvas.
    /// </summary>
    public static class BuildProfiles
    {
        private const string StandaloneOutput =
            "Build/Standalone/PocoRenderStudio.exe";

        private static readonly string PocoStudioOutput =
            Path.GetFullPath(
                Path.Combine(Application.dataPath,
                             "../../NewWork/poco_studio/prebuild/unity/PocoRenderStudio.exe"));

        // =====================================================================
        // Build for PocoStudio (Embedded): EMBEDDED_MODE for Qt hosting
        // =====================================================================

        [MenuItem("PocoRender/Build for PocoStudio (Embedded) %&b")]
        public static void BuildForPocoStudio()
        {
            SetDefines("EMBEDDED_MODE");

            string dir = Path.GetDirectoryName(PocoStudioOutput);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes = FindScenes(),
                locationPathName = PocoStudioOutput,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[BuildProfiles] PocoStudio Embedded build finished: {report.summary.result}" +
                      $"\n  Output: {PocoStudioOutput}" +
                      $"\n  EMBEDDED_MODE: YES (for Qt hosting)");
        }

        // =====================================================================
        // Standalone: independent development build
        // =====================================================================

        [MenuItem("PocoRender/Build Standalone (Independent)")]
        public static void BuildStandalone()
        {
            SetDefines("");

            var options = new BuildPlayerOptions
            {
                scenes = FindScenes(),
                locationPathName = StandaloneOutput,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[BuildProfiles] Standalone build finished: {report.summary.result}");
        }

        // =====================================================================
        // Legacy Embedded: stripped UI (not recommended for Plan A)
        // =====================================================================

        [MenuItem("PocoRender/Build Embedded (Legacy)")]
        public static void BuildEmbeddedLegacy()
        {
            SetDefines("EMBEDDED_MODE");

            string dir = Path.GetDirectoryName(PocoStudioOutput);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes = FindScenes(),
                locationPathName = PocoStudioOutput,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[BuildProfiles] Legacy Embedded build finished: {report.summary.result}" +
                      $" → {PocoStudioOutput}" +
                      $"\n  WARNING: This build has EMBEDDED_MODE — UI is stripped.");
        }

        private static void SetDefines(string defines)
        {
            // Preserve existing defines (e.g. HAS_SENTIS) and only
            // add/remove build-profile-specific ones like EMBEDDED_MODE.
            string existing = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone);

            var set = new System.Collections.Generic.HashSet<string>(
                existing.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));

            // Always remove build-profile toggles first
            set.Remove("EMBEDDED_MODE");

            // Then add whatever the caller requested
            if (!string.IsNullOrEmpty(defines))
            {
                foreach (var d in defines.Split(';'))
                {
                    var trimmed = d.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        set.Add(trimmed);
                }
            }

            string result = string.Join(";", set);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone, result);
            Debug.Log($"[BuildProfiles] Scripting defines set to: {result}");
        }

        private static string[] FindScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path))
                    list.Add(s.path);
            }

            if (list.Count == 0)
            {
                string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                        list.Add(path);
                }
            }

            if (list.Count == 0)
            {
                Debug.LogError("[BuildProfiles] No scenes found. " +
                               "Please add at least one scene to Build Settings " +
                               "or place a .unity file in Assets/Scenes/.");
            }
            else
            {
                Debug.Log($"[BuildProfiles] Building with {list.Count} scene(s): " +
                          string.Join(", ", list));
            }

            return list.ToArray();
        }
    }
}
#endif
