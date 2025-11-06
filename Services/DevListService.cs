using System;
using System.Collections.Generic;
using System.IO;

namespace SolanaPumpTracker.Services
{
    public static class DevListService
    {
        public const string WhitelistFileName = "whitelist_dev.txt";
        public const string BlacklistFileName = "blacklist_dev.txt";

        public static string ExeDir => AppContext.BaseDirectory;

        public static (HashSet<string> whitelist, HashSet<string> blacklist) Load(bool useWhitelist, bool useBlacklist)
        {
            var wl = useWhitelist ? ReadSet(Path.Combine(ExeDir, WhitelistFileName)) : new HashSet<string>();
            var bl = useBlacklist ? ReadSet(Path.Combine(ExeDir, BlacklistFileName)) : new HashSet<string>();
            return (wl, bl);
        }

        private static HashSet<string> ReadSet(string path)
        {
            var set = new HashSet<string>(StringComparer.Ordinal); // base58 чувствителен к регистру
            if (!File.Exists(path)) return set;

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#") || line.StartsWith(";")) continue;
                // поддержка инлайн-комментария: адрес # comment
                var idx = line.IndexOf('#');
                if (idx >= 0) line = line.Substring(0, idx).Trim();
                if (line.Length == 0) continue;

                set.Add(line);
            }
            return set;
        }
    }
}
