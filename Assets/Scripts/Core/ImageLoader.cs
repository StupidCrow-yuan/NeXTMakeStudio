using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System;

namespace NeXTMake.Core
{
    public class ImageLoader : MonoBehaviour
    {
        public delegate void ImageLoadCallback(Texture2D texture, string error);

        /// <summary>
        /// 异步加载图片（使用协程，不阻塞UI）
        /// </summary>
        public void LoadImageAsync(string imagePath, ImageLoadCallback callback)
        {
            StartCoroutine(LoadImageCoroutine(imagePath, callback));
        }

        /// <summary>
        /// 异步加载图片（使用Task，.NET 4.x+）
        /// </summary>
        public async Task<Texture2D> LoadImageTaskAsync(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    Debug.LogError($"File not found: {imagePath}");
                    return null;
                }

                // 在后台线程读取文件
                byte[] fileData = await Task.Run(() => File.ReadAllBytes(imagePath));

                // 回到主线程创建纹理
                return await CreateTextureFromBytes(fileData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading image: {e.Message}");
                return null;
            }
        }

        private async Task<Texture2D> CreateTextureFromBytes(byte[] fileData)
        {
            // 确保在主线程执行
            await Task.Yield();

            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(fileData);

            if (!loaded)
            {
                Destroy(texture);
                return null;
            }

            return texture;
        }

        IEnumerator LoadImageCoroutine(string imagePath, ImageLoadCallback callback)
        {
            // 验证文件
            if (!File.Exists(imagePath))
            {
                callback?.Invoke(null, $"File not found: {imagePath}");
                yield break;
            }

            // 显示加载进度
            yield return null;

            // 读取文件（分块读取，避免卡顿）
            byte[] fileData = null;
            Task readTask = Task.Run(() =>
            {
                try
                {
                    fileData = File.ReadAllBytes(imagePath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error reading file: {e.Message}");
                }
            });

            // 等待读取完成，但不阻塞主线程
            while (!readTask.IsCompleted)
            {
                yield return null; // 每帧检查一次
            }

            if (fileData == null)
            {
                callback?.Invoke(null, "Failed to read file");
                yield break;
            }

            // 创建纹理（在主线程）
            yield return null; // 让出一帧，保持UI响应

            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(fileData);

            if (loaded)
            {
                callback?.Invoke(texture, null);
            }
            else
            {
                Destroy(texture);
                callback?.Invoke(null, "Failed to load image data");
            }
        }

        /// <summary>
        /// 同步加载（不推荐，会阻塞UI）
        /// </summary>
        public Texture2D LoadImageSync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Debug.LogError("File not found: " + imagePath);
                return null;
            }

            byte[] fileData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(fileData))
            {
                return texture;
            }

            Destroy(texture);
            return null;
        }
    }
}