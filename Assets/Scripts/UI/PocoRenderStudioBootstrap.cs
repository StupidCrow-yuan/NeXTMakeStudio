using UnityEngine;
using UnityEngine.EventSystems;
using PocoRender.Core;
using PocoRender.Communication;
using PocoRender.UI.Core;

namespace PocoRender.UI
{
    /// <summary>
    /// Ensures PocoRender Studio UI is built when entering play mode, even if the scene
    /// doesn't already contain a PocoRenderStudioUIAutoSetup GameObject.
    ///
    /// In embedded mode, also attaches <see cref="QtBridgeController"/> so that
    /// the IPC layer is active before any UI is constructed.
    /// </summary>
    public static class PocoRenderStudioBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureUI()
        {
            // If UI manager already exists, assume UI is built.
            if (Object.FindObjectOfType<PocoRenderStudioUIManager>() != null)
                return;

            // Ensure there is an EventSystem so the selection dialog can be clicked.
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(es);
            }

            // Ensure there is a Canvas.
            UIFactory.FindOrCreateCanvas();

            // Create setup object and build UI once.
            var setup = new GameObject("PocoRenderStudioSetup");
            Object.DontDestroyOnLoad(setup);

            var auto = setup.AddComponent<PocoRenderStudioUIAutoSetup>();
            auto.autoCreateOnStart = false; // avoid double build from Start()
            auto.replaceOldUI = true;

            try
            {
                auto.SetupPocoRenderStudioUI();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrap] UI setup failed: {ex}");
            }

            if (BuildMode.IsEmbeddedMode)
            {
                // Legacy embedded mode: full bidirectional IPC
                var bridgeGo = new GameObject("QtBridgeController");
                Object.DontDestroyOnLoad(bridgeGo);
                bridgeGo.AddComponent<QtBridgeController>();
                Debug.Log("[Bootstrap] QtBridgeController created for embedded mode");
            }
            else if (BuildMode.HasPrintService)
            {
                // Plan A: standalone mode launched from PocoStudio
                var printGo = new GameObject("PrintButtonController");
                Object.DontDestroyOnLoad(printGo);
                printGo.AddComponent<PrintButtonController>();
                Debug.Log("[Bootstrap] PrintButtonController created (Plan A: print service port=" +
                          BuildMode.PrintServicePort + ")");
            }
        }
    }
}




