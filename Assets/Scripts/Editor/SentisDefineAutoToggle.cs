using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PocoRender.UI.EditorTools
{
    /// <summary>
    /// Keeps global scripting define HAS_SENTIS in sync with whether com.unity.sentis is installed.
    /// This avoids hard asmdef references to Unity.Sentis so the project can open even without Sentis.
    /// </summary>
    [InitializeOnLoad]
    public static class SentisDefineAutoToggle
    {
        private const string PackageName = "com.unity.sentis";
        private const string Define = "HAS_SENTIS";

        static SentisDefineAutoToggle()
        {
            // Delay to let PackageManager initialize.
            EditorApplication.delayCall += Sync;
        }

        private static void Sync()
        {
            bool hasSentis = false;
            try
            {
                var pkgs = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
                hasSentis = pkgs != null && pkgs.Any(p => string.Equals(p.name, PackageName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SentisDefineAutoToggle] Failed to query PackageInfo: " + e.Message);
                return;
            }

            // Keep a small set of common target groups in sync.
            var groups = new[]
            {
                BuildTargetGroup.Standalone,
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.WebGL,
                BuildTargetGroup.WSA
            };

            bool anyChanged = false;
            foreach (var g in groups)
            {
                if (g == BuildTargetGroup.Unknown) continue;
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(g);
                var list = symbols.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

                bool hasDefine = list.Contains(Define);
                if (hasSentis && !hasDefine)
                {
                    list.Add(Define);
                    anyChanged = true;
                }
                else if (!hasSentis && hasDefine)
                {
                    list.RemoveAll(s => s == Define);
                    anyChanged = true;
                }

                if (anyChanged)
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(g, string.Join(";", list));
                }
            }

            if (anyChanged)
            {
                Debug.Log($"[SentisDefineAutoToggle] {(hasSentis ? "Added" : "Removed")} {Define} (Sentis installed={hasSentis}). Recompiling scripts...");
            }
        }
    }
}


