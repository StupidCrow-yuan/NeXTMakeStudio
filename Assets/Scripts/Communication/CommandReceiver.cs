using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace PocoRender.Communication
{
    /// <summary>
    /// Lightweight TCP server that listens for QtCommand messages from the
    /// Qt host process. Uses the same length-prefixed JSON wire format as
    /// <see cref="EventSender"/>.
    ///
    /// Incoming command JSON strings are placed in a thread-safe queue and
    /// can be dequeued from the Unity main thread via <see cref="TryDequeue"/>.
    /// </summary>
    public class CommandReceiver
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenThread;
        private readonly ConcurrentQueue<string> _incomingCommands = new ConcurrentQueue<string>();
        private volatile bool _running;
        private int _port;

        public bool IsRunning => _running;
        public int Port => _port;

        public void Start(int port)
        {
            _port = port;
            _running = true;
            _listenThread = new Thread(ListenLoop) { IsBackground = true };
            _listenThread.Start();
            Debug.Log($"[CommandReceiver] Listening on port {port}");
        }

        public void Stop()
        {
            _running = false;
            try { _client?.Close(); } catch { }
            try { _listener?.Stop(); } catch { }
        }

        public bool TryDequeue(out string commandJson)
        {
            return _incomingCommands.TryDequeue(out commandJson);
        }

        private void ListenLoop()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();

                while (_running)
                {
                    if (!_listener.Pending())
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    _client = _listener.AcceptTcpClient();
                    _stream = _client.GetStream();
                    Debug.Log("[CommandReceiver] Qt host connected");

                    ReadLoop();
                }
            }
            catch (Exception ex)
            {
                if (_running)
                    Debug.LogError($"[CommandReceiver] Listen error: {ex.Message}");
            }
        }

        private void ReadLoop()
        {
            byte[] headerBuf = new byte[4];
            try
            {
                while (_running && _client.Connected)
                {
                    if (!ReadExact(headerBuf, 4)) break;

                    int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headerBuf, 0));
                    if (length <= 0 || length > 16 * 1024 * 1024) break;

                    byte[] payload = new byte[length];
                    if (!ReadExact(payload, length)) break;

                    string json = Encoding.UTF8.GetString(payload);
                    _incomingCommands.Enqueue(json);
                }
            }
            catch (Exception ex)
            {
                if (_running)
                    Debug.LogWarning($"[CommandReceiver] Read error: {ex.Message}");
            }
        }

        private bool ReadExact(byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read <= 0) return false;
                offset += read;
            }
            return true;
        }
    }
}
