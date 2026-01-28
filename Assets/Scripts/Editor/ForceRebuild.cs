using UnityEngine;
using UnityEditor;
using NeXTMake.UI;

namespace NeXTMake.Editor
{
    public class ForceRebuild
    {
        [MenuItem("Tools/NeXTMake Studio/DEBUG FORCE REBUILD")]
        public static void Rebuild()
        {
            Debug.ClearDeveloperConsole();
            Debug.Log("--- FORCE REBUILD START ---");

            // Destroy everything
            var objs = GameObject.FindObjectsOfType<GameObject>();
            foreach(var o in objs)
            {
                if (o.name.Contains("UIContainer") || o.name.Contains("Canvas") || o.name == "NeXTMakeStudioSetup")
                {
                    GameObject.DestroyImmediate(o);
                }
            }

            // Create new setup
            GameObject setup = new GameObject("NeXTMakeStudioSetup");
            var script = setup.AddComponent<NeXTMakeStudioUIAutoSetup>();
            
            // Force run
            script.SetupNeXTMakeStudioUI();

            Debug.Log("--- FORCE REBUILD DONE ---");
        }
    }
}

