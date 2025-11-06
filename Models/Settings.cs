namespace SolanaPumpTracker.Models
{
    public sealed class Settings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string WebSocketEndpoint { get; set; } = "";

        // Фильтры — старые
        public double MinDevMigrationPct { get; set; } = 0.0;
        public int MinMigratedTokens { get; set; } = 0;
        public bool RequireLastDevTokenMigrated { get; set; } = false;

        // Фильтры — новые (token/pair/twitter)
        public bool PostOnly { get; set; } = false;      // показывать только с твитом
        public bool ShowTweetText { get; set; } = false; // выводить текст твита в карточке
        public bool AutoOpenInBrowser { get; set; } = false;
        public bool UseWhitelistFromFile { get; set; } = false;
        public bool UseBlacklistFromFile { get; set; } = false;
        public int MinNumHolders { get; set; } = 0;
        public double MaxTop10HoldersPct { get; set; } = 100.0;
        public double MaxDevHoldsPct { get; set; } = 100.0;
        public double MaxSnipersHoldPct { get; set; } = 100.0;

        public int MinAuthorFollowers { get; set; } = 0;
        public int MaxTweetAgeMinutes { get; set; } = 1440;

        // Поток
        public bool IgnoreHistoryOnStart { get; set; } = true;
        public int TimeSkewSeconds { get; set; } = 5;
        public bool AutoReconnect { get; set; } = true;

        // UI
        public int MaxItems { get; set; } = 30;
    }
}
