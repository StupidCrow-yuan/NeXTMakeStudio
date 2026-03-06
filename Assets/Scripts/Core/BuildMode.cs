using System;
using UnityEngine;

namespace PocoRender.Core
{
    /// <summary>
    /// Determines whether the application is running in standalone mode
    /// (full Unity UI) or embedded mode (hosted inside PocoStudio Qt).
    ///
    /// Plan A architecture: Unity always runs as an independent window.
    /// When launched from PocoStudio, it receives --print-service-port
    /// so it knows where to send print requests.
    /// Legacy --embedded-mode / --ipc-port are still parsed for backward compat.
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

        /// <summary>
        /// Port of PocoStudio's PrintServiceListener (Plan A).
        /// 0 means no Qt print service was specified (Unity launched standalone).
        /// </summary>
        public static int PrintServicePort { get; private set; } = 0;

        /// <summary>True when launched from PocoStudio with a valid print service port.</summary>
        public static bool HasPrintService => PrintServicePort > 0;

        /// <summary>
        /// Parent window handle passed via -parentHWND.  Non-zero means Unity
        /// was asked to create its render surface as a child of this HWND.
        /// </summary>
        public static IntPtr ParentHwnd { get; private set; } = IntPtr.Zero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DetectMode()
        {
            bool hasEmbeddedArg = false;
            bool hasPrintServiceArg = false;

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--embedded-mode")
                {
                    hasEmbeddedArg = true;
                }
                else if (args[i] == "--standalone-mode")
                {
                    _isEmbeddedMode = false;
                }
                else if (args[i] == "--ipc-port" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int port))
                        IpcPort = port;
                }
                else if (args[i] == "--print-service-port" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int port))
                    {
                        PrintServicePort = port;
                        hasPrintServiceArg = true;
                    }
                }
                else if (args[i] == "-parentHWND" && i + 1 < args.Length)
                {
                    if (long.TryParse(args[i + 1], out long hwnd))
                        ParentHwnd = new IntPtr(hwnd);
                }
            }

            if (hasEmbeddedArg)
            {
                _isEmbeddedMode = true;
            }
            else if (hasPrintServiceArg && !hasEmbeddedArg)
            {
                // Plan A: launched from Qt with print service but NOT embedded mode.
                // Force standalone even if compiled with EMBEDDED_MODE define.
                _isEmbeddedMode = false;
            }

            Debug.Log($"[BuildMode] mode={(_isEmbeddedMode ? "Embedded" : "Standalone")} " +
                      $"ipcPort={IpcPort} printServicePort={PrintServicePort} " +
                      $"hasPrintService={HasPrintService}");
        }
    }
}
