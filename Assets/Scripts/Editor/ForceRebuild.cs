using UnityEngine;
using UnityEditor;
using PocoRender.UI;

namespace PocoRender.Editor
{
    public class ForceRebuild
    {
        [MenuItem("Tools/PocoRender Studio/DEBUG FORCE REBUILD")]
        public static void Rebuild()
        {
            Debug.ClearDeveloperConsole();
            Debug.Log("--- FORCE REBUILD START ---");

            // Destroy everything
            var objs = GameObject.FindObjectsOfType<GameObject>();
            foreach(var o in objs)
            {
                if (o.name.Contains("UIContainer") || o.name.Contains("Canvas") || o.name == "PocoRenderStudioSetup")
                {
                    GameObject.DestroyImmediate(o);
                }
            }

            // Create new setup
            GameObject setup = new GameObject("PocoRenderStudioSetup");
            var script = setup.AddComponent<PocoRenderStudioUIAutoSetup>();
            
            // Force run
            script.SetupPocoRenderStudioUI();

            Debug.Log("--- FORCE REBUILD DONE ---");
        }
    }
}



