using SolanaPumpTracker.Models;
using SolanaPumpTracker.Services;
using System;
using System.Globalization;
using System.Windows;

namespace SolanaPumpTracker.Views
{
    public partial class ConfigWindow : Window
    {
        private Settings _s = new();

        public ConfigWindow()
        {
            InitializeComponent();
        }

        public void LoadFrom(Settings s)
        {
            EndpointBox.Text = s.WebSocketEndpoint ?? "";
            ApiKeyBox.Password = s.ApiKey ?? "";

            AutoReconnectBox.IsChecked = s.AutoReconnect;

            IgnoreHistoryCheck.IsChecked = s.IgnoreHistoryOnStart;
            TimeSkewBox.Text = s.TimeSkewSeconds.ToString();
            MaxItemsBox.Text = s.MaxItems.ToString();
            AutoOpenInBrowserBox.IsChecked = s.AutoOpenInBrowser;

            MinDevMigrationPctBox.Text = s.MinDevMigrationPct.ToString();
            MinMigratedTokensBox.Text = s.MinMigratedTokens.ToString();
            RequireLastDevTokenMigratedBox.IsChecked = s.RequireLastDevTokenMigrated;

            MinNumHoldersBox.Text = s.MinNumHolders.ToString();
            MaxTop10HoldersPctBox.Text = s.MaxTop10HoldersPct.ToString();
            MaxDevHoldsPctBox.Text = s.MaxDevHoldsPct.ToString();
            MaxSnipersHoldPctBox.Text = s.MaxSnipersHoldPct.ToString();

            PostOnlyBox.IsChecked = s.PostOnly;
            ShowTweetTextBox.IsChecked = s.ShowTweetText;
            MinAuthorFollowersBox.Text = s.MinAuthorFollowers.ToString();
            MaxTweetAgeMinutesBox.Text = s.MaxTweetAgeMinutes.ToString();

            UseWhitelistFromFileBox.IsChecked = s.UseWhitelistFromFile;
            UseBlacklistFromFileBox.IsChecked = s.UseBlacklistFromFile;
        }

        public Settings SaveTo()
        {
            var s = SettingsService.Load();
            s.WebSocketEndpoint = EndpointBox.Text.Trim();
            s.ApiKey = ApiKeyBox.Password ?? "";


            s.WebSocketEndpoint = EndpointBox.Text.Trim();
            s.ApiKey = ApiKeyBox.Password;
            s.AutoReconnect = AutoReconnectBox.IsChecked == true;

            s.IgnoreHistoryOnStart = IgnoreHistoryCheck.IsChecked == true;
            s.AutoOpenInBrowser = AutoOpenInBrowserBox.IsChecked == true;

            s.PostOnly = PostOnlyBox.IsChecked == true;
            s.ShowTweetText = ShowTweetTextBox.IsChecked == true;

            s.UseWhitelistFromFile = UseWhitelistFromFileBox.IsChecked == true;
            s.UseBlacklistFromFile = UseBlacklistFromFileBox.IsChecked == true;
            

            // числовые (с безопасным парсингом)
            if (int.TryParse(TimeSkewBox.Text, out var skew)) s.TimeSkewSeconds = Math.Max(0, skew);
            if (int.TryParse(MaxItemsBox.Text, out var mi)) s.MaxItems = Math.Max(1, mi);

            if (double.TryParse(MinDevMigrationPctBox.Text, out var d1)) s.MinDevMigrationPct = Math.Max(0, d1);
            if (int.TryParse(MinMigratedTokensBox.Text, out var d2)) s.MinMigratedTokens = Math.Max(0, d2);
            if (int.TryParse(MinNumHoldersBox.Text, out var d4)) s.MinNumHolders = Math.Max(0, d4);
            if (double.TryParse(MaxTop10HoldersPctBox.Text, out var d5)) s.MaxTop10HoldersPct = Math.Max(0, d5);
            if (double.TryParse(MaxDevHoldsPctBox.Text, out var d6)) s.MaxDevHoldsPct = Math.Max(0, d6);
            if (double.TryParse(MaxSnipersHoldPctBox.Text, out var d7)) s.MaxSnipersHoldPct = Math.Max(0, d7);

            if (long.TryParse(MinAuthorFollowersBox.Text, out var af)) s.MinAuthorFollowers = (int)Math.Max(0, af);
            if (int.TryParse(MaxTweetAgeMinutesBox.Text, out var ta)) s.MaxTweetAgeMinutes = Math.Max(0, ta);

            return s;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }


        private static int ParseInt(string? s, int def)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;

        private static double ParseDouble(string? s, double def)
            => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
