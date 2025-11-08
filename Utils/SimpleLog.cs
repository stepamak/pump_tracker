using System;
using System.Diagnostics;
using System.IO;

namespace SolanaPumpTracker.Utils
{
    public static class SimpleLog
    {
        public static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolanaPumpTracker");
        public static readonly string AppDataLogPath = Path.Combine(AppDataDir, "log.txt");

        public static readonly string ExeDir = AppContext.BaseDirectory;
        public static readonly string ExeLogPath = Path.Combine(ExeDir, "log.txt");

        public static void Info(string msg)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}\n";
            try { Directory.CreateDirectory(AppDataDir); File.AppendAllText(AppDataLogPath, line); } catch { }
            try { File.AppendAllText(ExeLogPath, line); } catch { }
        }

        public static void OpenAppDataFolder()
        {
            try { Directory.CreateDirectory(AppDataDir); Process.Start(new ProcessStartInfo(AppDataDir) { UseShellExecute = true }); } catch { }
        }

        public static void OpenExeFolder()
        {
            try { Process.Start(new ProcessStartInfo(ExeDir) { UseShellExecute = true }); } catch { }
        }

        public static void Error(string msg)
        {
            Info("ERROR: " + msg);
        }
    }
}
