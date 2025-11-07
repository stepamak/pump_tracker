using SolanaPumpTracker.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SolanaPumpTracker.Services
{
    public static class DevListService
    {
        public const string WhitelistFileName = "whitelist_dev.txt";
        public const string BlacklistFileName = "blacklist_dev.txt";

        public static readonly string ExeDir =
            AppDomain.CurrentDomain.BaseDirectory;

        public static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolanaPumpTracker");


        public static (HashSet<string> whitelist, HashSet<string> blacklist) Load(bool useWhitelist, bool useBlacklist)
        {
            var wl = new HashSet<string>(StringComparer.Ordinal);
            var bl = new HashSet<string>(StringComparer.Ordinal);

            if (useWhitelist)
            {
                foreach (var p in CandidatePaths(WhitelistFileName))
                    ReadIntoSet(p, wl);
            }
            if (useBlacklist)
            {
                foreach (var p in CandidatePaths(BlacklistFileName))
                    ReadIntoSet(p, bl);
            }
            return (wl, bl);
        }

        public static (string triedPath, string usedPath) AddToWhitelist(string dev) =>
            AddToFile(WhitelistFileName, dev);

        public static (string triedPath, string usedPath) AddToBlacklist(string dev) =>
            AddToFile(BlacklistFileName, dev);
        private static (string triedPath, string usedPath) AddToFile(string fileName, string dev)
        {
            var exePath = Path.Combine(ExeDir, fileName);
            var ok = TryAppendUniqueLine(exePath, dev, out var used);
            if (ok) return (exePath, used!);

            var appDataPath = Path.Combine(AppDataDir, fileName);
            Directory.CreateDirectory(AppDataDir);
            TryAppendUniqueLine(appDataPath, dev, out used);
            return (exePath, used!);
        }

        private static bool TryAppendUniqueLine(string path, string dev, out string? usedPath)
        {
            usedPath = null;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var existing = new HashSet<string>(StringComparer.Ordinal);
                if (File.Exists(path))
                {
                    foreach (var l in File.ReadLines(path, Encoding.UTF8))
                    {
                        var t = l.Trim();
                        if (t.Length == 0 || t.StartsWith("#")) continue;
                        existing.Add(t);
                    }
                }

                if (!existing.Contains(dev))
                {
                    using var sw = new StreamWriter(path, append: true, new UTF8Encoding(false));
                    if (!File.Exists(path) || new FileInfo(path).Length == 0)
                        sw.WriteLine("# one dev address per line");
                    sw.WriteLine(dev);
                }

                usedPath = path;
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // нет прав на запись в exe-dir
                return false;
            }
            catch (Exception ex)
            {
                // другие ошибки логируем и считаем неуспехом
                SimpleLog.Error($"DevListService: write failed {path}: {ex.Message}");
                return false;
            }
        }

        private static void ReadIntoSet(string path, HashSet<string> set)
        {
            try
            {
                if (!File.Exists(path)) return;
                foreach (var l in File.ReadLines(path, Encoding.UTF8))
                {
                    var t = l.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    set.Add(t);
                }
            }
            catch (Exception ex)
            {
                SimpleLog.Error($"DevListService: read failed {path}: {ex.Message}");
            }
        }

        private static IEnumerable<string> CandidatePaths(string fileName)
        {
            yield return Path.Combine(ExeDir, fileName);
            yield return Path.Combine(AppDataDir, fileName);
        }
    }
}
