using UnityEngine;
using System.IO;
using System;

namespace NeXTMake.Utils
{
    public class Logger : MonoBehaviour
    {
        private static Logger instance;
        private string logPath;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                logPath = Path.Combine(Application.persistentDataPath, "app_log.txt");
                Application.logMessageReceived += HandleLog;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            try
            {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{type}] {logString}\n";

                if (type == LogType.Error || type == LogType.Exception)
                {
                    logEntry += $"Stack Trace: {stackTrace}\n";
                }

                File.AppendAllText(logPath, logEntry);
            }
            catch { }
        }

        public static void Log(string message)
        {
            Debug.Log($"[Logger] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[Logger] {message}");
        }
    }
}