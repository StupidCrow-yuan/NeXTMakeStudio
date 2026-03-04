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
    ///   2. Build for PocoStudio (Plan A) — full Unity UI, NO EMBEDDED_MODE.
    ///      Output: PocoStudio/prebuild/unity/PocoRenderStudio.exe
    ///      When launched from Qt with --print-service-port, shows "Send to Print" button.
    ///
    ///   3. Build Embedded (Legacy) — stripped UI with EMBEDDED_MODE define.
    ///      Kept for backward compatibility but NOT recommended for Plan A.
    /// </summary>
    public static class BuildProfiles
    {
        private const string StandaloneOutput =
            "Build/Standalone/PocoRenderStudio.exe";

        private static readonly string PocoStudioOutput =
            Path.GetFullPath(
                Path.Combine(Application.dataPath,
                             "../../PocoStudio/prebuild/unity/PocoRenderStudio.exe"));

        // =====================================================================
        // Plan A (recommended): Full UI build for PocoStudio integration
        // =====================================================================

        [MenuItem("PocoRender/Build for PocoStudio (Plan A) %&b")]
        public static void BuildForPocoStudio()
        {
            // No EMBEDDED_MODE — full UI preserved
            SetDefines("");

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
            Debug.Log($"[BuildProfiles] Plan A build finished: {report.summary.result}" +
                      $"\n  Output: {PocoStudioOutput}" +
                      $"\n  Full UI: YES  |  EMBEDDED_MODE: NO" +
                      $"\n  Launch from Qt with --print-service-port to enable print button");
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
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone, defines);
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
