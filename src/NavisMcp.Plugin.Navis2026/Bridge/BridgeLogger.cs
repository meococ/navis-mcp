using System;
using System.IO;

namespace NavisMcp.Plugin.Navis2026.Bridge
{
    internal sealed class BridgeLogger
    {
        private readonly object _gate = new object();
        private readonly string _filePath;

        public BridgeLogger(string targetId)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logPath = Path.Combine(localAppData, "NavisMcp", "logs");
            Directory.CreateDirectory(logPath);
            _filePath = Path.Combine(logPath, "plugin-" + targetId + ".log");
        }

        public void Info(string message)
        {
            Write("INFO", message, null);
        }

        public void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private void Write(string level, string message, Exception ex)
        {
            try
            {
                var line = DateTimeOffset.UtcNow.ToString("O") + " [" + level + "] " + message;
                if (ex != null)
                {
                    line += Environment.NewLine + ex;
                }

                lock (_gate)
                {
                    File.AppendAllText(_filePath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never block Navisworks startup or UI work.
            }
        }
    }
}
