using System;
using UnityEngine;

namespace PocoRender.Core
{
    /// <summary>
    /// Determines whether the application is running in standalone mode
    /// (full Unity UI) or embedded mode (hosted inside PocoStudio Qt).
    ///
    /// Detection happens via two mechanisms:
    ///   1. Compile-time: the EMBEDDED_MODE scripting define symbol.
    ///   2. Runtime: the --embedded-mode command-line argument (takes priority).
    ///
    /// In embedded mode, application-level UI (File menu, project browser)
    /// is hidden because Qt provides those. The Canvas editor, layers panel,
    /// tools, and 3D preview remain fully active inside the Unity process.
    /// </summary>
    public static class BuildMode
    {
#if EMBEDDED_MODE
        private static bool _isEmbeddedMode = true;
#else
        private static bool _isEmbeddedMode = false;
#endif

        public static bool IsEmbeddedMode
        {
            get => _isEmbeddedMode;
            private set => _isEmbeddedMode = value;
        }

        public static bool IsStandaloneMode => !_isEmbeddedMode;

        /// <summary>IPC port passed via --ipc-port. Only meaningful in embedded mode.</summary>
        public static int IpcPort { get; private set; } = 50051;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DetectMode()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--embedded-mode")
                {
                    _isEmbeddedMode = true;
                }
                else if (args[i] == "--ipc-port" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int port))
                        IpcPort = port;
                }
            }

            Debug.Log($"[BuildMode] mode={(_isEmbeddedMode ? "Embedded" : "Standalone")} ipcPort={IpcPort}");
        }
    }
}
