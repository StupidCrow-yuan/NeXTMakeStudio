using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections;

namespace PocoRender.Communication
{
    public class CppBridge : MonoBehaviour
    {
        [Header("Plugin Settings")]
        public string pluginName = "NativeBridge";

        // C++��������
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const string PLUGIN_NAME = "NativeBridge";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private const string PLUGIN_NAME = "libNativeBridge";
#else
        private const string PLUGIN_NAME = "NativeBridge";
#endif

        // C++��������
        [DllImport(PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int InitializeBridge();

        [DllImport(PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CleanupBridge();

        [DllImport(PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SendMessage(string message, int messageLength);

        [DllImport(PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int ProcessImageData(IntPtr imageData, int width, int height, int channels);

        [DllImport(PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetLastError();

        // �첽�����ص�
        public delegate void ProcessImageCallback(ProcessResult result);
        public delegate void MessageCallback(bool success, string error);

        private bool isInitialized = false;
        private bool isProcessing = false;

        public bool IsInitialized => isInitialized;
        public bool IsProcessing => isProcessing;

        void Start()
        {
            // �����������Զ���ʼ�������ֶ�����
            // Initialize();
        }

        /// <summary>
        /// ��ʼ��C++�Žӣ��첽��
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (isInitialized)
                return true;

            try
            {
                // �ں�̨�̳߳�ʼ��
                int result = await Task.Run(() => 
                {
                    try
                    {
                        return InitializeBridge();
                    }
                    catch (DllNotFoundException)
                    {
                        // C++插件不存在，这是正常的，不报错
                        return -1;
                    }
                    catch (Exception)
                    {
                        return -1;
                    }
                });

                isInitialized = (result == 0);

                if (!isInitialized)
                {
                    // 如果插件不存在，只记录警告，不报错
                    Debug.LogWarning("C++ bridge plugin not found. Some features may be unavailable.");
                }

                return isInitialized;
            }
            catch (Exception e)
            {
                // 只记录警告，不报错
                Debug.LogWarning("C++ bridge initialization skipped: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// ��ʼ��C++�Žӣ�ͬ����ʹ��Э�̣�
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            StartCoroutine(InitializeCoroutine());
        }

        IEnumerator InitializeCoroutine()
        {
            Task<bool> initTask = InitializeAsync();

            while (!initTask.IsCompleted)
            {
                yield return null; // �ȴ���ɣ���������
            }

            isInitialized = initTask.Result;
        }

        public void Cleanup()
        {
            if (isInitialized)
            {
                try
                {
                    CleanupBridge();
                    isInitialized = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("Exception cleaning up C++ bridge: " + e.Message);
                }
            }
        }

        /// <summary>
        /// �첽����ͼƬ������Ϣ
        /// </summary>
        public async Task<bool> OnImageLoadedAsync(string imagePath)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("C++ bridge not initialized");
                return false;
            }

            try
            {
                string message = "IMAGE_LOADED:" + imagePath;

                // �ں�̨�̷߳���
                int result = await Task.Run(() => SendMessage(message, message.Length));

                if (result != 0)
                {
                    Debug.LogWarning("Failed to send image loaded message");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Exception sending image loaded message: " + e.Message);
                return false;
            }
        }

        public void OnImageLoaded(string imagePath)
        {
            StartCoroutine(OnImageLoadedCoroutine(imagePath));
        }

        IEnumerator OnImageLoadedCoroutine(string imagePath)
        {
            Task<bool> task = OnImageLoadedAsync(imagePath);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            // ������ɣ����Դ������
        }

        /// <summary>
        /// �첽����ͼƬ��������UI��
        /// </summary>
        public async Task<ProcessResult> ProcessImageAsync(Texture2D texture)
        {
            if (!isInitialized)
            {
                return new ProcessResult
                {
                    success = false,
                    error = "C++ bridge not initialized"
                };
            }

            if (texture == null)
            {
                return new ProcessResult
                {
                    success = false,
                    error = "Texture is null"
                };
            }

            if (isProcessing)
            {
                return new ProcessResult
                {
                    success = false,
                    error = "Already processing an image"
                };
            }

            isProcessing = true;

            try
            {
                // ��ȡ�������ݣ������̣߳�
                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                int height = texture.height;

                // ת��Ϊ�ֽ����飨�ں�̨�̣߳�
                byte[] imageData = await Task.Run(() =>
                {
                    byte[] data = new byte[width * height * 4];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        data[i * 4] = pixels[i].r;
                        data[i * 4 + 1] = pixels[i].g;
                        data[i * 4 + 2] = pixels[i].b;
                        data[i * 4 + 3] = pixels[i].a;
                    }
                    return data;
                });

                // ����C++�������ں�̨�̣߳�
                int result = await Task.Run(() =>
                {
                    IntPtr ptr = Marshal.AllocHGlobal(imageData.Length);
                    try
                    {
                        Marshal.Copy(imageData, 0, ptr, imageData.Length);
                        int processResult = ProcessImageData(ptr, width, height, 4);
                        return processResult;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                });

                isProcessing = false;

                if (result == 0)
                {
                    return new ProcessResult { success = true };
                }
                else
                {
                    string error = GetLastErrorString();
                    return new ProcessResult { success = false, error = error };
                }
            }
            catch (Exception e)
            {
                isProcessing = false;
                return new ProcessResult { success = false, error = e.Message };
            }
        }

        /// <summary>
        /// ʹ��Э�̴���ͼƬ��������UI��
        /// </summary>
        public void ProcessImage(Texture2D texture, ProcessImageCallback callback)
        {
            StartCoroutine(ProcessImageCoroutine(texture, callback));
        }

        IEnumerator ProcessImageCoroutine(Texture2D texture, ProcessImageCallback callback)
        {
            Task<ProcessResult> task = ProcessImageAsync(texture);

            // �ȴ�������ɣ�ÿ֡���һ��
            while (!task.IsCompleted)
            {
                yield return null; // ������UI
            }

            // ������ɣ����ûص�
            callback?.Invoke(task.Result);
        }

        /// <summary>
        /// �첽������Ϣ
        /// </summary>
        public async Task<bool> SendMessageAsync(string message)
        {
            if (!isInitialized)
                return false;

            try
            {
                int result = await Task.Run(() => SendMessage(message, message.Length));
                return result == 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Exception sending message: " + e.Message);
                return false;
            }
        }

        public bool SendMessageToCpp(string message)
        {
            StartCoroutine(SendMessageCoroutine(message));
            return true; // �������أ�ʵ�ʽ��ͨ���ص�����
        }

        IEnumerator SendMessageCoroutine(string message)
        {
            Task<bool> task = SendMessageAsync(message);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            // ���������ﴦ�����
            if (!task.Result)
            {
                Debug.LogWarning("Failed to send message: " + message);
            }
        }

        private string GetLastErrorString()
        {
            try
            {
                IntPtr errorPtr = GetLastError();
                if (errorPtr != IntPtr.Zero)
                {
                    return Marshal.PtrToStringAnsi(errorPtr);
                }
            }
            catch { }
            return "Unknown error";
        }

        void OnDestroy()
        {
            Cleanup();
        }
    }

    // ��������ṹ
    public struct ProcessResult
    {
        public bool success;
        public string error;
    }
}
