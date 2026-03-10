using System;
using System.Runtime.InteropServices;
using UnityEngine;
using PocoRender.Core;
using PocoRender.UI;

namespace PocoRender.Communication
{
    /// <summary>
    /// Main controller that orchestrates communication between Unity and the
    /// PocoStudio Qt host process when running in embedded mode.
    ///
    /// Wires together:
    ///   - <see cref="CommandReceiver"/>: TCP server receiving QtCommand JSON
    ///   - <see cref="EventSender"/>: TCP client pushing UnityEvent JSON
    ///   - <see cref="CanvasController"/>: the canvas editing logic
    ///
    /// In standalone mode this component disables itself and does nothing.
    /// </summary>
    public class QtBridgeController : MonoBehaviour
    {
#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
#endif

        [Header("IPC Settings")]
        [Tooltip("Port Unity listens on for commands from Qt (overridden by --ipc-port)")]
        public int commandPort = 50051;
        [Tooltip("Port Unity connects to for sending events to Qt (commandPort + 1)")]
        public int eventPort = 50052;

        private CommandReceiver _receiver;
        private EventSender _sender;
        private CommandDispatcher _dispatcher;

        private bool _initialized;
        public event Action<string, bool, string, string> OnConvertToPngResult;
        /// <summary>Fired when Qt sends back the result of an open-file-dialog request (standalone/embedded).</summary>
        public event Action<string, bool, string> OnOpenFileDialogResult;

        public bool IsConnected => _sender != null && _sender.IsConnected;

        void Awake()
        {
#if UNITY_EDITOR
            // Never run embedded IPC in the Unity Editor — no Qt host is present.
            Debug.Log("[QtBridgeController] Editor mode — embedded IPC disabled");
            enabled = false;
            return;
#endif

            if (BuildMode.IsStandaloneMode)
            {
                Debug.Log("[QtBridgeController] Standalone mode — embedded IPC disabled");
                enabled = false;
                return;
            }

#if UNITY_STANDALONE_WIN
            EmbeddedWindowHider.HideAllProcessWindows();
#endif

            commandPort = BuildMode.IpcPort;
            eventPort = commandPort + 1;
        }

        void Start()
        {
            if (!enabled) return;

            _receiver = new CommandReceiver();
            _sender = new EventSender();
            _dispatcher = new CommandDispatcher();

            _receiver.Start(commandPort);

            bool connected = _sender.Connect("127.0.0.1", eventPort);
            if (!connected)
            {
                Debug.LogWarning("[QtBridgeController] Could not connect event channel — will retry");
            }

            SendUnityReadyEvent();
            _initialized = true;

            Debug.Log($"[QtBridgeController] Embedded IPC started cmd={commandPort} evt={eventPort}");
        }

        void Update()
        {
            if (!_initialized) return;

            while (_receiver.TryDequeue(out string json))
            {
                try
                {
                    _dispatcher.Dispatch(json, _sender);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[QtBridgeController] Command dispatch error: {ex.Message}");
                    SendErrorEvent("command_dispatch", ex.Message);
                }
            }

            _sender.FlushPending();
        }

        void OnDestroy()
        {
            _receiver?.Stop();
            _sender?.Disconnect();
        }

        public void SendUnityReadyEvent()
        {
            long hwnd = DetectMainWindowHwnd();
            string json = JsonUtility.ToJson(new UnityReadyPayload
            {
                type = "unity_ready",
                unity_version = Application.version,
                unity_hwnd = hwnd
            });
            _sender?.QueueEvent(json);
            Debug.Log($"[QtBridgeController] Sent unity_ready hwnd=0x{hwnd:X}");
        }

        /// <summary>
        /// Detects the main Unity player window HWND by enumerating windows
        /// owned by the current process.
        /// </summary>
        private static long DetectMainWindowHwnd()
        {
#if UNITY_STANDALONE_WIN
            try
            {
                uint pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                IntPtr found = IntPtr.Zero;

                // Strategy 1: if launched with -parentHWND, find the child window
                // inside the Qt container (this is the expected path).
                if (BuildMode.ParentHwnd != IntPtr.Zero)
                {
                    EnumChildWindows(BuildMode.ParentHwnd, (hWnd, _) =>
                    {
                        GetWindowThreadProcessId(hWnd, out uint winPid);
                        if (winPid == pid)
                        {
                            found = hWnd;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (found != IntPtr.Zero)
                        return found.ToInt64();
                }

                // Strategy 2: GetActiveWindow (works for both top-level and child)
                IntPtr active = GetActiveWindow();
                if (active != IntPtr.Zero)
                    return active.ToInt64();

                // Strategy 3: Process.MainWindowHandle
                IntPtr mainWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (mainWnd != IntPtr.Zero)
                    return mainWnd.ToInt64();

                // Strategy 4: enumerate top-level windows (fallback)
                EnumWindows((hWnd, _) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint winPid);
                    if (winPid == pid)
                    {
                        int len = GetWindowTextLength(hWnd);
                        if (len > 0)
                        {
                            found = hWnd;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                return found.ToInt64();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QtBridgeController] HWND detection failed: {ex.Message}");
                return 0;
            }
#else
            return 0;
#endif
        }

        public void SendUndoStackChanged(bool canUndo, bool canRedo,
                                          int undoCount, int redoCount,
                                          string lastAction)
        {
            string json = JsonUtility.ToJson(new UndoStackPayload
            {
                type = "undo_stack_changed",
                can_undo = canUndo,
                can_redo = canRedo,
                undo_count = undoCount,
                redo_count = redoCount,
                last_action_name = lastAction ?? ""
            });
            _sender?.QueueEvent(json);
        }

        public void SendProjectStateChanged(bool isModified, string projectName,
                                             int layerCount)
        {
            string json = JsonUtility.ToJson(new ProjectStatePayload
            {
                type = "project_state_changed",
                is_modified = isModified,
                project_name = projectName ?? "",
                layer_count = layerCount
            });
            _sender?.QueueEvent(json);
        }

        public void SendSelectionChanged(string objectId, string objectName,
                                          float x, float y, float w, float h,
                                          float rotation, bool locked, bool visible)
        {
            string json = JsonUtility.ToJson(new SelectionPayload
            {
                type = "selection_changed",
                object_id = objectId ?? "",
                object_name = objectName ?? "",
                pos_x = x, pos_y = y,
                width = w, height = h,
                rotation = rotation,
                is_locked = locked,
                is_visible = visible
            });
            _sender?.QueueEvent(json);
        }

        public void SendProgressUpdate(string operation, float progress, string text)
        {
            string json = JsonUtility.ToJson(new ProgressPayload
            {
                type = "progress",
                operation = operation ?? "",
                progress = progress,
                status_text = text ?? ""
            });
            _sender?.QueueEvent(json);
        }

        public void SendErrorEvent(string context, string message)
        {
            string json = JsonUtility.ToJson(new ErrorPayload
            {
                type = "error",
                level = 2,
                message = message ?? "",
                details = context ?? ""
            });
            _sender?.QueueEvent(json);
        }

        public bool SendConvertToPngRequest(string requestId, string inputPath)
        {
            if (_sender == null || !_sender.IsConnected) return false;
            if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(inputPath)) return false;

            string json = JsonUtility.ToJson(new ConvertToPngRequestPayload
            {
                type = "convert_to_png_request",
                request_id = requestId,
                input_path = inputPath
            });
            _sender.QueueEvent(json);
            return true;
        }

        public void NotifyConvertToPngResult(string requestId, bool success, string outputPngPath, string errorMessage)
        {
            OnConvertToPngResult?.Invoke(requestId, success, outputPngPath ?? "", errorMessage ?? "");
        }

        public bool SendOpenFileDialogRequest(string requestId, string title, string filter)
        {
            if (_sender == null || !_sender.IsConnected) return false;
            if (string.IsNullOrEmpty(requestId)) return false;

            string json = JsonUtility.ToJson(new OpenFileDialogRequestPayload
            {
                type = "open_file_dialog_request",
                request_id = requestId,
                title = title ?? "Select File",
                filter = filter ?? ""
            });
            _sender.QueueEvent(json);
            return true;
        }

        public void NotifyOpenFileDialogResult(string requestId, bool success, string filePath)
        {
            OnOpenFileDialogResult?.Invoke(requestId, success, filePath ?? "");
        }

        public void SendPublishWork(int width, int height, string thumbBase64, string projectJson, PublishMetadata metadata)
        {
            string json = JsonUtility.ToJson(new PublishWorkPayload
            {
                type = "publish_work",
                width = width,
                height = height,
                thumbnail_base64 = thumbBase64,
                project_json = projectJson,
                metadata = metadata
            });
            _sender?.QueueEvent(json);
        }

        [Serializable] public struct PublishMetadata
        {
            public string name;
            public string category;
            public string theme;
            public string style;
            public string license;
            public string tags;
        }

        [Serializable] private struct UnityReadyPayload
        {
            public string type;
            public string unity_version;
            public long unity_hwnd;
        }

        [Serializable] private struct UndoStackPayload
        {
            public string type;
            public bool can_undo;
            public bool can_redo;
            public int undo_count;
            public int redo_count;
            public string last_action_name;
        }

        [Serializable] private struct ProjectStatePayload
        {
            public string type;
            public bool is_modified;
            public string project_name;
            public int layer_count;
        }

        [Serializable] private struct SelectionPayload
        {
            public string type;
            public string object_id;
            public string object_name;
            public float pos_x;
            public float pos_y;
            public float width;
            public float height;
            public float rotation;
            public bool is_locked;
            public bool is_visible;
        }

        [Serializable] private struct ProgressPayload
        {
            public string type;
            public string operation;
            public float progress;
            public string status_text;
        }

        [Serializable] private struct ErrorPayload
        {
            public string type;
            public int level;
            public string message;
            public string details;
        }

        [Serializable] private struct ConvertToPngRequestPayload
        {
            public string type;
            public string request_id;
            public string input_path;
        }

        [Serializable] private struct OpenFileDialogRequestPayload
        {
            public string type;
            public string request_id;
            public string title;
            public string filter;
        }

        [Serializable] private struct PublishWorkPayload
        {
            public string type;
            public int width;
            public int height;
            public string thumbnail_base64;
            public string project_json;
            public PublishMetadata metadata;
        }
    }
}
