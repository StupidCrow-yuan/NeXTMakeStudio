using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PocoRender.Core
{
    /// <summary>
    /// Runs at the earliest C# entry point (BeforeSceneLoad) to hide the Unity
    /// window when launched in embedded mode (--embedded-mode).  This prevents
    /// the Unity window from flashing on-screen before the Qt host controls
    /// its visibility.
    ///
    /// Works together with Qt-side STARTF_USESHOWWINDOW and SetWinEventHook
    /// to provide defence-in-depth against window flash.
    /// </summary>
    public static class EmbeddedWindowHider
    {
#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const int SW_HIDE = 0;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HideIfEmbedded()
        {
            string[] args = Environment.GetCommandLineArgs();
            bool isEmbedded = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--embedded-mode")
                {
                    isEmbedded = true;
                    break;
                }
            }

            if (!isEmbedded) return;

#if UNITY_STANDALONE_WIN
            HideAllProcessWindows();
            Debug.Log("[EmbeddedWindowHider] All process windows hidden at BeforeSceneLoad");
#endif
        }

#if UNITY_STANDALONE_WIN
        /// <summary>
        /// Hides all visible top-level windows belonging to this process.
        /// Safe to call from any context.
        /// </summary>
        public static void HideAllProcessWindows()
        {
            uint pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint winPid);
                if (winPid == pid && IsWindowVisible(hWnd))
                    ShowWindow(hWnd, SW_HIDE);
                return true;
            }, IntPtr.Zero);
        }
#endif
    }
}
