using System;
using UnityEngine;
using UnityEngine.UI;
using PocoRender.Core;
using PocoRender.UI;
using PocoRender.UI.Core;
using PocoRender.UI.Modules;

namespace PocoRender.Communication
{
    /// <summary>
    /// Parses incoming QtCommand JSON messages and dispatches them to the
    /// appropriate Unity subsystem (CanvasController, etc.).
    ///
    /// Wire format: the same length-prefixed JSON used by
    /// <see cref="CommandReceiver"/> / <see cref="EventSender"/>.
    ///
    /// JSON schema example:
    /// <code>{ "command": "undo" }</code>
    /// <code>{ "command": "new_project", "project_name": "My Art", "canvas_width": 600, "canvas_height": 600 }</code>
    /// </summary>
    public class CommandDispatcher
    {
        private CanvasController _canvas;
        private string _currentProjectName = "";
        private bool _isModified;

        public void Dispatch(string json, EventSender sender)
        {
            EnsureCanvas();

            var msg = JsonUtility.FromJson<QtCommandMessage>(json);
            if (msg == null || string.IsNullOrEmpty(msg.command))
            {
                Debug.LogWarning($"[CommandDispatcher] Unknown message: {json}");
                return;
            }

            switch (msg.command)
            {
                case "undo":
                    _canvas?.Undo();
                    SendAck(sender, "undo", true);
                    break;

                case "redo":
                    _canvas?.Redo();
                    SendAck(sender, "redo", true);
                    break;

                case "new_project":
                    HandleNewProject(msg);
                    SendAck(sender, "new_project", true);
                    break;

                case "open_project":
                    HandleOpenProject(msg);
                    SendAck(sender, "open_project", true);
                    break;

                case "save_project":
                    HandleSaveProject(msg, sender);
                    break;

                case "close_project":
                    HandleCloseProject(msg);
                    SendAck(sender, "close_project", true);
                    break;

                case "set_view_mode":
                    HandleSetViewMode(msg);
                    SendAck(sender, "set_view_mode", true);
                    break;

                case "export":
                    HandleExport(msg, sender);
                    break;

                case "shutdown":
                    Debug.Log("[CommandDispatcher] Shutdown requested by Qt host");
                    SendAck(sender, "shutdown", true);
                    sender.FlushPending();
                    Application.Quit();
                    break;

                case "convert_to_png_result":
                    HandleConvertToPngResult(msg);
                    SendAck(sender, "convert_to_png_result", true);
                    break;

                case "ping":
                    SendAck(sender, "pong", true);
                    break;

                case "open_file_dialog_result":
                    HandleOpenFileDialogResult(msg);
                    break;

                default:
                    Debug.LogWarning($"[CommandDispatcher] Unrecognized command: {msg.command}");
                    SendAck(sender, msg.command, false, "unrecognized command");
                    break;
            }
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            // Prefer the canvas that HomeModule considers active (set whenever a tab
            // is created or switched), then fall back to scene-wide search.
            _canvas = HomeModule.ActiveController
                      ?? UnityEngine.Object.FindObjectOfType<CanvasController>();
        }

        /// <summary>
        /// Returns true when a canvas has no user-placed objects (only the internal
        /// BGDeselector helper, if present). Used to decide whether to reuse the
        /// startup blank canvas or to create a new tab.
        /// </summary>
        private static bool IsCanvasEmpty(CanvasController canvas)
        {
            if (canvas == null || canvas.paper == null) return true;
            for (int i = 0; i < canvas.paper.childCount; i++)
            {
                if (canvas.paper.GetChild(i).name != "BGDeselector")
                    return false;
            }
            return true;
        }

        private void HandleNewProject(QtCommandMessage msg)
        {
            Debug.Log($"[CommandDispatcher] NewProject: {msg.project_name} " +
                      $"{msg.canvas_width}x{msg.canvas_height} mode={msg.mode} template={msg.template_id}");

            SwitchToMode(msg.mode);

            if (BuildMode.IsEmbeddedMode)
            {
                // Get the currently active canvas (set by HomeModule when a tab is
                // created or switched).
                _canvas = HomeModule.ActiveController;
                if (_canvas == null) EnsureCanvas();

                if (_canvas == null || !IsCanvasEmpty(_canvas))
                {
                    // No canvas yet, OR active canvas already has user content
                    // → create a fresh blank tab (same as clicking "+" in Unity).
                    if (HomeModule.AddCanvasAction != null)
                        HomeModule.AddCanvasAction(null);
                    _canvas = HomeModule.ActiveController;
                    if (_canvas == null) EnsureCanvas();
                }
                // else: active canvas is empty (e.g. startup blank canvas) → reuse it.

                // Clear any leftover content so the canvas is truly blank.
                if (_canvas?.paper != null)
                {
                    for (int i = _canvas.paper.childCount - 1; i >= 0; i--)
                    {
                        var child = _canvas.paper.GetChild(i);
                        if (child.name == "BGDeselector") continue;
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                }
            }
            else
            {
                // Standalone mode: create a new canvas tab (same as the "+" button).
                if (HomeModule.AddCanvasAction != null)
                    HomeModule.AddCanvasAction(null);

                // Re-lookup the newly created (now active) CanvasController
                _canvas = null;
                EnsureCanvas();
            }

            if (_canvas == null)
            {
                Debug.LogWarning("[CommandDispatcher] CanvasController not found after AddCanvas");
                return;
            }

            _currentProjectName = msg.project_name ?? "Untitled";

            var paper = _canvas.paper;
            if (paper != null)
            {
                float w = msg.canvas_width > 0 ? msg.canvas_width : 600;
                float h = msg.canvas_height > 0 ? msg.canvas_height : 600;
                paper.sizeDelta = new Vector2(w, h);

                if (!string.IsNullOrEmpty(msg.template_id))
                {
                    ApplyTemplateToPaper(msg.template_id, paper);
                }
            }

            _isModified = false;
        }

        private void ApplyTemplateToPaper(string templateId, RectTransform paper)
        {
            if (paper == null || string.IsNullOrEmpty(templateId)) return;

            Texture2D tex = null;

            // If templateId looks like an absolute file path, load from disk
            if (templateId.Length > 2 && templateId[1] == ':' || templateId.StartsWith("/"))
            {
                tex = LoadTextureFromFile(templateId);
            }
            else
            {
                // Fallback: treat as Unity resource name
                tex = Resources.Load<Texture2D>(templateId);
            }

            if (tex == null)
            {
                Debug.LogWarning($"[CommandDispatcher] ApplyTemplate: could not load image for '{templateId}'");
                return;
            }

            GameObject addedImg = UIFactory.CreateObject("ImportedDesign", paper.gameObject);
            RectTransform rt = addedImg.GetComponent<RectTransform>();

            // Fit the image within the paper while preserving aspect ratio
            float maxW = paper.sizeDelta.x * 0.9f;
            float maxH = paper.sizeDelta.y * 0.9f;
            float scale = Mathf.Min(maxW / tex.width, maxH / tex.height, 1f);
            rt.sizeDelta = new Vector2(tex.width * scale, tex.height * scale);
            rt.anchoredPosition = Vector2.zero;

            Image img = addedImg.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0.5f, 0.5f));
            img.preserveAspect = true;

            addedImg.AddComponent<CanvasRenderer>();
            addedImg.AddComponent<CanvasGroup>();
            addedImg.AddComponent<BoxCollider2D>();
            addedImg.AddComponent<ObjectManipulator>();

            _canvas?.RecordAdd(addedImg);
        }

        private static Texture2D LoadTextureFromFile(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    Debug.LogWarning($"[CommandDispatcher] Image file not found: {path}");
                    return null;
                }
                byte[] data = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data))
                    return tex;
                UnityEngine.Object.Destroy(tex);
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CommandDispatcher] Failed to load image '{path}': {ex.Message}");
                return null;
            }
        }

        private void HandleOpenProject(QtCommandMessage msg)
        {
            Debug.Log($"[CommandDispatcher] OpenProject: {msg.project_path} mode={msg.mode}");

            // Switch to the requested editor mode FIRST, then re-lookup canvas.
            SwitchToMode(msg.mode);

            _canvas = HomeModule.ActiveController;
            if (_canvas == null) EnsureCanvas();

            if (_canvas == null || !IsCanvasEmpty(_canvas))
            {
                // No canvas, or active canvas already has content → create a new tab
                // so the opened project gets its own dedicated canvas.
                if (HomeModule.AddCanvasAction != null)
                    HomeModule.AddCanvasAction(null);
                _canvas = HomeModule.ActiveController;
                if (_canvas == null) EnsureCanvas();
            }
            // else: active canvas is empty (startup blank) → load project into it.

            if (!string.IsNullOrEmpty(msg.project_data))
            {
                try
                {
                    string json = System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(msg.project_data));
                    var project = ProjectSerializer.Deserialize(json);
                    if (project != null && _canvas != null && _canvas.paper != null)
                    {
                        ProjectSerializer.ApplyToCanvas(project, _canvas.paper);
                        _currentProjectName = project.project_name;
                        _isModified = false;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CommandDispatcher] OpenProject deserialization failed: {ex.Message}");
                }
            }
        }

        private void HandleSaveProject(QtCommandMessage msg, EventSender sender)
        {
            EnsureCanvas();
            Debug.Log($"[CommandDispatcher] SaveProject: {msg.save_path} saveAs={msg.save_as}");

            string projectJson = "";
            int dataSize = 0;
            string checksum = "";

            if (_canvas != null && _canvas.paper != null)
            {
                projectJson = ProjectSerializer.Serialize(
                    _canvas.paper,
                    _currentProjectName,
                    _canvas.paper.sizeDelta.x,
                    _canvas.paper.sizeDelta.y);

                byte[] data = System.Text.Encoding.UTF8.GetBytes(projectJson);
                dataSize = data.Length;
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(data);
                    checksum = System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }

                _isModified = false;
            }

            string response = JsonUtility.ToJson(new ProjectDataReadyPayload
            {
                type = "project_data_ready",
                save_path = msg.save_path ?? "",
                data_size = dataSize,
                checksum = checksum,
                project_data_b64 = System.Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(projectJson))
            });
            sender?.QueueEvent(response);
        }

        private void HandleCloseProject(QtCommandMessage msg)
        {
            EnsureCanvas();
            Debug.Log($"[CommandDispatcher] CloseProject force={msg.force}");

            if (_canvas != null && _canvas.paper != null)
            {
                for (int i = _canvas.paper.childCount - 1; i >= 0; i--)
                {
                    var child = _canvas.paper.GetChild(i);
                    if (child.name == "BGDeselector") continue;
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
            _currentProjectName = "";
            _isModified = false;
        }

        private void SwitchToMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return;
            var uiMgr = UnityEngine.Object.FindObjectOfType<PocoRenderStudioUIManager>();
            if (uiMgr == null) return;

            switch (mode)
            {
                case "uv_print":
                    uiMgr.SetCurrentMode(PocoRender.Core.PrintMode.UVPrint);
                    break;
                case "3d_print":
                    uiMgr.SetCurrentMode(PocoRender.Core.PrintMode.Print3D);
                    break;
                case "uv_3d_print":
                    uiMgr.SetCurrentMode(PocoRender.Core.PrintMode.UVPrint);
                    break;
                default:
                    uiMgr.SetCurrentMode(PocoRender.Core.PrintMode.UVPrint);
                    break;
            }
        }

        private void HandleSetViewMode(QtCommandMessage msg)
        {
            Debug.Log($"[CommandDispatcher] SetViewMode: {msg.view_mode}");
            var uiMgr = UnityEngine.Object.FindObjectOfType<PocoRenderStudioUIManager>();
            if (uiMgr == null) return;

            switch (msg.view_mode)
            {
                case "home":
                    uiMgr.ShowSelectionDialog();
                    break;
                case "canvas":
                    uiMgr.SetCurrentMode(PocoRender.Core.PrintMode.UVPrint);
                    break;
                case "preview_3d":
                    uiMgr.SetCurrentMode(PocoRender.Core.PrintMode.Print3D);
                    break;
            }
        }

        private void HandleExport(QtCommandMessage msg, EventSender sender)
        {
            EnsureCanvas();
            Debug.Log($"[CommandDispatcher] Export: {msg.output_path} fmt={msg.format} dpi={msg.dpi}");
            SendAck(sender, "export", true);
        }

        private void HandleConvertToPngResult(QtCommandMessage msg)
        {
            var bridge = UnityEngine.Object.FindObjectOfType<QtBridgeController>();
            if (bridge == null) return;

            bridge.NotifyConvertToPngResult(
                msg.request_id ?? "",
                msg.success,
                msg.output_png_path ?? "",
                msg.error_message ?? "");
        }

        private void HandleOpenFileDialogResult(QtCommandMessage msg)
        {
            var bridge = UnityEngine.Object.FindObjectOfType<QtBridgeController>();
            if (bridge != null)
                bridge.NotifyOpenFileDialogResult(
                    msg.request_id ?? "",
                    msg.success,
                    msg.file_path ?? "");
        }

        private void SendAck(EventSender sender, string command, bool success,
                              string error = null)
        {
            if (sender == null) return;
            string json = JsonUtility.ToJson(new AckPayload
            {
                type = "command_ack",
                command = command,
                success = success,
                error = error ?? ""
            });
            sender.QueueEvent(json);
        }

        [Serializable]
        private class QtCommandMessage
        {
            public string command;
            public string project_name;
            public int canvas_width;
            public int canvas_height;
            public string template_id;
            public string mode;
            public string project_path;
            public string project_data;
            public string save_path;
            public bool save_as;
            public bool force;
            public string view_mode;
            public string output_path;
            public string format;
            public int dpi;
            public string request_id;
            public bool success;
            public string output_png_path;
            public string error_message;
            public string file_path;
        }

        [Serializable]
        private struct AckPayload
        {
            public string type;
            public string command;
            public bool success;
            public string error;
        }

        [Serializable]
        private struct ProjectDataReadyPayload
        {
            public string type;
            public string save_path;
            public int data_size;
            public string checksum;
            public string project_data_b64;
        }
    }
}
