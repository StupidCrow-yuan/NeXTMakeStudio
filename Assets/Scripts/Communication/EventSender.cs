using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace PocoRender.Communication
{
    /// <summary>
    /// Lightweight TCP-based event sender that pushes UnityEvent messages to
    /// the Qt host. Uses a simple length-prefixed JSON protocol.
    ///
    /// Wire format (per message):
    ///   [4-byte big-endian length][UTF-8 JSON payload]
    ///
    /// Qt's QtUnityBridge reads from this same socket.
    /// </summary>
    public class EventSender
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly ConcurrentQueue<string> _pendingEvents = new ConcurrentQueue<string>();
        private bool _connected;

        public bool IsConnected => _connected;

        public bool Connect(string host, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(host, port);
                _stream = _client.GetStream();
                _connected = true;
                Debug.Log($"[EventSender] Connected to {host}:{port}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EventSender] Connection failed: {ex.Message}");
                _connected = false;
                return false;
            }
        }

        public void QueueEvent(string eventJson)
        {
            _pendingEvents.Enqueue(eventJson);
        }

        /// <summary>Send all queued events. Safe to call every frame.</summary>
        public void FlushPending()
        {
            if (!_connected || _stream == null) return;

            while (_pendingEvents.TryDequeue(out string json))
            {
                try
                {
                    byte[] payload = Encoding.UTF8.GetBytes(json);
                    byte[] header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
                    _stream.Write(header, 0, 4);
                    _stream.Write(payload, 0, payload.Length);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventSender] Send failed: {ex.Message}");
                    _connected = false;
                    return;
                }
            }
        }

        public void Disconnect()
        {
            _connected = false;
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
        }
    }
}
