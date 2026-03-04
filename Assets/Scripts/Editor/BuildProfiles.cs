#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace PocoRender.Editor
{
    /// <summary>
    /// Provides menu-driven build profiles for the two deployment modes:
    ///   1. Standalone — full Unity UI, no IPC, for independent development.
    ///   2. Embedded  — stripped-down UI, IPC enabled, output goes to
    ///      PocoStudio/prebuild/unity/ so the Qt host can launch it.
    /// </summary>
    public static class BuildProfiles
    {
        private const string StandaloneOutput =
            "Build/Standalone/PocoRenderStudio.exe";

        private static readonly string EmbeddedOutput =
            Path.GetFullPath(
                Path.Combine(Application.dataPath,
                             "../../../../PocoStudio/prebuild/unity/PocoRenderStudio.exe"));

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

        [MenuItem("PocoRender/Build Embedded (for PocoStudio)")]
        public static void BuildEmbedded()
        {
            SetDefines("EMBEDDED_MODE");

            string dir = Path.GetDirectoryName(EmbeddedOutput);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes = FindScenes(),
                locationPathName = EmbeddedOutput,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[BuildProfiles] Embedded build finished: {report.summary.result}" +
                      $" → {EmbeddedOutput}");
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
                // Auto-discover scene files under Assets/Scenes/
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
