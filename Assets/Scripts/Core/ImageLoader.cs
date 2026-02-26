using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System;

namespace PocoRender.Core
{
    public class ImageLoader : MonoBehaviour
    {
        public delegate void ImageLoadCallback(Texture2D texture, string error);

        /// <summary>
        /// �첽����ͼƬ��ʹ��Э�̣�������UI��
        /// </summary>
        public void LoadImageAsync(string imagePath, ImageLoadCallback callback)
        {
            StartCoroutine(LoadImageCoroutine(imagePath, callback));
        }

        /// <summary>
        /// �첽����ͼƬ��ʹ��Task��.NET 4.x+��
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

                // �ں�̨�̶߳�ȡ�ļ�
                byte[] fileData = await Task.Run(() => File.ReadAllBytes(imagePath));

                // �ص����̴߳�������
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
            // ȷ�������߳�ִ��
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
            // ��֤�ļ�
            if (!File.Exists(imagePath))
            {
                callback?.Invoke(null, $"File not found: {imagePath}");
                yield break;
            }

            // ��ʾ���ؽ���
            yield return null;

            // ��ȡ�ļ����ֿ��ȡ�����⿨�٣�
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

            // �ȴ���ȡ��ɣ������������߳�
            while (!readTask.IsCompleted)
            {
                yield return null; // ÿ֡���һ��
            }

            if (fileData == null)
            {
                callback?.Invoke(null, "Failed to read file");
                yield break;
            }

            // ��������������̣߳�
            yield return null; // �ó�һ֡������UI��Ӧ

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
        /// ͬ�����أ����Ƽ���������UI��
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
