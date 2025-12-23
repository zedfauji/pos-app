using System;
using System.IO;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Services
{
    public static class DebugLogger
    {
        private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MagiDesk_Payment_Debug.log");
        private static readonly object LockObject = new object();

        public static void Log(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}";
                
                lock (LockObject)
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // If logging fails, we can't do much about it
                // But we'll try to write to a fallback location
                try
                {
                    var fallbackPath = Path.Combine(Path.GetTempPath(), "MagiDesk_Payment_Debug.log");
                    File.AppendAllText(fallbackPath, $"[{DateTime.Now}] LOGGING FAILED: {ex.Message} - Original: {message}" + Environment.NewLine);
                }
                catch
                {
                    // If even fallback fails, ignore it
                }
            }
        }

        public static void LogException(string context, Exception ex)
        {
            Log($"EXCEPTION in {context}: {ex.Message}");
            Log($"STACK TRACE: {ex.StackTrace}");
        }

        public static void LogStep(string step, string details = "")
        {
            Log($"STEP: {step} - {details}");
        }

        public static void LogMethodEntry(string methodName, string parameters = "")
        {
            Log($"ENTER: {methodName}({parameters})");
        }

        public static void LogMethodExit(string methodName, string result = "")
        {
            Log($"EXIT: {methodName} - {result}");
        }

        public static void ClearLog()
        {
            try
            {
                lock (LockObject)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                }
            }
            catch
            {
                // Ignore if we can't clear the log
            }
        }
    }
}
