using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System;

namespace PocoRender.Utils
{
    /// <summary>
    /// MakerWorld下载助手，提供下载和文件管理功能
    /// </summary>
    public class MakerWorldDownloadHelper : MonoBehaviour
    {
        [Header("下载设置")]
        public string modelsFolder = "3DModels";
        public string defaultFileName = "model.stl";

        /// <summary>
        /// 获取3D模型文件夹路径
        /// </summary>
        public string GetModelsFolderPath()
        {
            string folderPath = Path.Combine(Application.dataPath, modelsFolder);
            
            // 确保文件夹存在
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    Directory.CreateDirectory(folderPath);
                    Debug.Log($"[MakerWorldDownloadHelper] 创建文件夹: {folderPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MakerWorldDownloadHelper] 创建文件夹失败: {e.Message}");
                    return null;
                }
            }
            
            return folderPath;
        }

        /// <summary>
        /// 从URL下载模型文件
        /// </summary>
        public async Task<string> DownloadModelFromUrlAsync(string url, string fileName = null)
        {
            try
            {
                string folderPath = GetModelsFolderPath();
                if (folderPath == null)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    // 从URL提取文件名
                    fileName = ExtractFileNameFromUrl(url);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = defaultFileName;
                    }
                }

                string savePath = Path.Combine(folderPath, fileName);
                
                Debug.Log($"[MakerWorldDownloadHelper] 开始下载: {url}");
                Debug.Log($"[MakerWorldDownloadHelper] 保存路径: {savePath}");

                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    // 设置User-Agent，避免被服务器拒绝
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    
                    // 下载文件
                    await client.DownloadFileTaskAsync(new Uri(url), savePath);
                    
                    Debug.Log($"[MakerWorldDownloadHelper] 下载完成: {fileName}");
                    return savePath;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MakerWorldDownloadHelper] 下载失败: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 从URL提取文件名
        /// </summary>
        string ExtractFileNameFromUrl(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);
                
                // 如果URL包含查询参数，尝试从路径中提取
                if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
                {
                    // 尝试从URL路径的最后一部分获取
                    string[] pathParts = uri.LocalPath.Split('/');
                    foreach (string part in pathParts)
                    {
                        if (part.Contains(".") && (part.EndsWith(".stl") || part.EndsWith(".obj")))
                        {
                            fileName = part;
                            break;
                        }
                    }
                    
                    if (string.IsNullOrEmpty(fileName) || !fileName.Contains("."))
                    {
                        fileName = defaultFileName;
                    }
                }
                
                return fileName;
            }
            catch
            {
                return defaultFileName;
            }
        }

        /// <summary>
        /// 复制文件到3D模型文件夹
        /// </summary>
        public string CopyModelFileToFolder(string sourcePath, string fileName = null)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    Debug.LogError($"[MakerWorldDownloadHelper] 源文件不存在: {sourcePath}");
                    return null;
                }

                string folderPath = GetModelsFolderPath();
                if (folderPath == null)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Path.GetFileName(sourcePath);
                }

                string destPath = Path.Combine(folderPath, fileName);
                
                // 如果目标文件已存在，添加序号
                int counter = 1;
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                while (File.Exists(destPath))
                {
                    fileName = $"{baseName}_{counter}{extension}";
                    destPath = Path.Combine(folderPath, fileName);
                    counter++;
                }

                File.Copy(sourcePath, destPath, false);
                Debug.Log($"[MakerWorldDownloadHelper] 文件已复制: {destPath}");
                
                return destPath;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MakerWorldDownloadHelper] 复制文件失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取文件夹中的所有模型文件
        /// </summary>
        public string[] GetModelFiles()
        {
            string folderPath = GetModelsFolderPath();
            if (folderPath == null || !Directory.Exists(folderPath))
            {
                return new string[0];
            }

            try
            {
                string[] stlFiles = Directory.GetFiles(folderPath, "*.stl", SearchOption.TopDirectoryOnly);
                string[] objFiles = Directory.GetFiles(folderPath, "*.obj", SearchOption.TopDirectoryOnly);
                
                string[] allFiles = new string[stlFiles.Length + objFiles.Length];
                Array.Copy(stlFiles, allFiles, stlFiles.Length);
                Array.Copy(objFiles, 0, allFiles, stlFiles.Length, objFiles.Length);
                
                return allFiles;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MakerWorldDownloadHelper] 获取模型文件列表失败: {e.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// 打开3D模型文件夹（在文件管理器中）
        /// </summary>
        public void OpenModelsFolder()
        {
            string folderPath = GetModelsFolderPath();
            if (folderPath != null && Directory.Exists(folderPath))
            {
                try
                {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    System.Diagnostics.Process.Start("explorer.exe", folderPath);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                    System.Diagnostics.Process.Start("open", folderPath);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                    System.Diagnostics.Process.Start("xdg-open", folderPath);
#else
                    Application.OpenURL("file://" + folderPath);
#endif
                    Debug.Log($"[MakerWorldDownloadHelper] 已打开文件夹: {folderPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MakerWorldDownloadHelper] 打开文件夹失败: {e.Message}");
                }
            }
        }
    }
}


