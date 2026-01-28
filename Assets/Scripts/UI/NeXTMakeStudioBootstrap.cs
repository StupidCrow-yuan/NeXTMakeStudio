using UnityEngine;
using UnityEngine.EventSystems;
using NeXTMake.UI.Core;

namespace NeXTMake.UI
{
    /// <summary>
    /// Ensures NeXTMake Studio UI is built when entering play mode, even if the scene
    /// doesn't already contain a NeXTMakeStudioUIAutoSetup GameObject.
    /// </summary>
    public static class NeXTMakeStudioBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureUI()
        {
            // If UI manager already exists, assume UI is built.
            if (Object.FindObjectOfType<NeXTMakeStudioUIManager>() != null)
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
            var setup = new GameObject("NeXTMakeStudioSetup");
            Object.DontDestroyOnLoad(setup);

            var auto = setup.AddComponent<NeXTMakeStudioUIAutoSetup>();
            auto.autoCreateOnStart = false; // avoid double build from Start()
            auto.replaceOldUI = true;

            auto.SetupNeXTMakeStudioUI();
        }
    }
}


