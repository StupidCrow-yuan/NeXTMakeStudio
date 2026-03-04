using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace PocoRender.Communication
{
    /// <summary>
    /// TCP client that sends print requests to PocoStudio's PrintServiceListener.
    /// Uses the same length-prefixed JSON wire format as the rest of the IPC.
    ///
    /// JSON schema sent:
    /// {
    ///   "type": "print_request",
    ///   "project_name": "...",
    ///   "image_path": "C:/...",           // optional (file on disk)
    ///   "image_data_b64": "...",           // optional (base64 PNG)
    ///   "width": 600,
    ///   "height": 600,
    ///   "dpi": 300,
    ///   "copies": 1
    /// }
    ///
    /// Response from Qt:
    /// { "type": "print_ack", "success": true, "message": "..." }
    /// </summary>
    public class PrintClient
    {
        [Serializable]
        public class PrintOptions
        {
            public int dpi = 300;
            public int copies = 1;
            public string paper_size = "A4";
            public string color_profile = "CMYK";
            public string media_type = "plain";
            public bool mirror_print = false;
            public string color_mode = "CMYK";
            public bool enable_halftone = true;
            public bool enable_ink_optimization = false;
            public bool enable_skin_detection = true;
            public bool enable_guided_filter = true;
            public bool show_ink_preview = true;
        }

        private static PrintClient _instance;
        public static PrintClient Instance => _instance ??= new PrintClient();

        private TcpClient _client;
        private NetworkStream _stream;
        private bool _connected;

        public bool IsConnected => _connected;

        public bool Connect(string host, int port)
        {
            try
            {
                Disconnect();
                _client = new TcpClient();
                _client.Connect(host, port);
                _stream = _client.GetStream();
                _connected = true;
                Debug.Log($"[PrintClient] Connected to PocoStudio at {host}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PrintClient] Connection failed: {ex.Message}");
                _connected = false;
                return false;
            }
        }

        /// <summary>
        /// Send a print request with an image file path on disk.
        /// </summary>
        public bool SendPrintRequest(string projectName, string imagePath,
                                     int width, int height, int dpi = 300,
                                     int copies = 1)
        {
            var payload = new PrintRequestPayload
            {
                type = "print_request",
                project_name = projectName ?? "Untitled",
                image_path = imagePath ?? "",
                image_data_b64 = "",
                width = width,
                height = height,
                dpi = dpi,
                copies = copies
            };
            return SendJson(JsonUtility.ToJson(payload));
        }

        /// <summary>
        /// Send a print request with base64-encoded image data.
        /// </summary>
        public bool SendPrintRequestWithData(string projectName, byte[] pngData,
                                             int width, int height, int dpi = 300,
                                             int copies = 1)
        {
            var options = new PrintOptions { dpi = dpi, copies = copies };
            return SendPrintRequestWithData(projectName, pngData, width, height, options);
        }

        public bool SendPrintRequestWithData(string projectName, byte[] pngData,
                                             int width, int height, PrintOptions options)
        {
            options ??= new PrintOptions();
            var payload = new PrintRequestPayload
            {
                type = "print_request",
                project_name = projectName ?? "Untitled",
                image_path = "",
                image_data_b64 = Convert.ToBase64String(pngData),
                width = width,
                height = height,
                dpi = options.dpi,
                copies = options.copies,
                paper_size = options.paper_size ?? "A4",
                color_profile = options.color_profile ?? "CMYK",
                media_type = options.media_type ?? "plain",
                mirror_print = options.mirror_print,
                color_mode = options.color_mode ?? "CMYK",
                enable_halftone = options.enable_halftone,
                enable_ink_optimization = options.enable_ink_optimization,
                enable_skin_detection = options.enable_skin_detection,
                enable_guided_filter = options.enable_guided_filter,
                show_ink_preview = options.show_ink_preview
            };
            return SendJson(JsonUtility.ToJson(payload));
        }

        private bool SendJson(string json)
        {
            if (!_connected || _stream == null)
            {
                Debug.LogWarning("[PrintClient] Not connected to PocoStudio");
                return false;
            }

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(json);
                byte[] header = BitConverter.GetBytes(
                    IPAddress.HostToNetworkOrder(payload.Length));
                _stream.Write(header, 0, 4);
                _stream.Write(payload, 0, payload.Length);
                _stream.Flush();
                Debug.Log($"[PrintClient] Sent print request ({payload.Length} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PrintClient] Send failed: {ex.Message}");
                _connected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            _connected = false;
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
        }

        [Serializable]
        private struct PrintRequestPayload
        {
            public string type;
            public string project_name;
            public string image_path;
            public string image_data_b64;
            public int width;
            public int height;
            public int dpi;
            public int copies;
            public string paper_size;
            public string color_profile;
            public string media_type;
            public bool mirror_print;
            public string color_mode;
            public bool enable_halftone;
            public bool enable_ink_optimization;
            public bool enable_skin_detection;
            public bool enable_guided_filter;
            public bool show_ink_preview;
        }
    }
}
