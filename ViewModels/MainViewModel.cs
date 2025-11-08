using SolanaPumpTracker.Models;
using SolanaPumpTracker.Services;
using SolanaPumpTracker.Services;
using SolanaPumpTracker.Utils;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;


namespace SolanaPumpTracker.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {

        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        private string _pingDisplay = "--";
        public string PingDisplay
        {
            get => _pingDisplay;
            private set { _pingDisplay = value; OnPropertyChanged(); }
        }

        private HashSet<string> _devWhitelist = new(StringComparer.Ordinal);
        private HashSet<string> _devBlacklist = new(StringComparer.Ordinal);

        private readonly WebSocketService _ws = new WebSocketService();
        private CancellationTokenSource? _cts;
        private DateTime _startedAtUtc = DateTime.MinValue;
        public ICommand OpenWhitelistFileCommand => new RelayCommand(_ => OpenDevFile(DevListService.WhitelistFileName));
        public ICommand OpenBlacklistFileCommand => new RelayCommand(_ => OpenDevFile(DevListService.BlacklistFileName));

        private Settings _settings = SettingsService.Load();
        public Settings Settings { get => _settings; set { _settings = value; OnPropertyChanged(); } }

        public ObservableCollection<TokenItemViewModel> Tokens { get; } = new();

        private bool _isConnected;
        public bool IsConnected { get => _isConnected; private set { _isConnected = value; OnPropertyChanged(); } }

        private string _status = "Disconnected";
        public string Status { get => _status; private set { _status = value; OnPropertyChanged(); } }
        public ICommand AddDevToWhitelistCommand { get; }
        public ICommand AddDevToBlacklistCommand { get; }

        // Диагностика
        private long _rxCount;
        public long RxCount { get => _rxCount; private set { _rxCount = value; OnPropertyChanged(); } }

        private long _parsedCount;
        public long ParsedCount { get => _parsedCount; private set { _parsedCount = value; OnPropertyChanged(); } }

        private long _errorCount;
        public long ErrorCount { get => _errorCount; private set { _errorCount = value; OnPropertyChanged(); } }

        private string _lastJsonPreview = string.Empty;
        public string LastJsonPreview { get => _lastJsonPreview; private set { _lastJsonPreview = value; OnPropertyChanged(); } }

        // Команды
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand OpenConfigCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand OpenLogAppDataCommand { get; }
        public ICommand OpenLogExeCommand { get; }
        public ICommand WriteTestLogCommand { get; }
        public ICommand AddTestTokenCommand { get; }

        public string LogPathAppData => SimpleLog.AppDataLogPath;
        public string LogPathExe => SimpleLog.ExeLogPath;

        public MainViewModel()
        {
            StartCommand = new RelayCommand(async _ => await StartAsync(), _ => !IsConnected);
            StopCommand = new RelayCommand(async _ => await StopAsync(), _ => IsConnected);
            OpenConfigCommand = new RelayCommand(_ => OpenConfig());
            ClearCommand = new RelayCommand(_ => Tokens.Clear());

            OpenLogAppDataCommand = new RelayCommand(_ => SimpleLog.OpenAppDataFolder());
            OpenLogExeCommand = new RelayCommand(_ => SimpleLog.OpenExeFolder());
            WriteTestLogCommand = new RelayCommand(_ => SimpleLog.Info("TEST LOG ENTRY"));
            AddTestTokenCommand = new RelayCommand(_ => AddTestToken());
            AddDevToWhitelistCommand = new RelayCommand(p => AddDevToList(p as TokenItemViewModel, true));
            AddDevToBlacklistCommand = new RelayCommand(p => AddDevToList(p as TokenItemViewModel, false));

            _ws.MessageReceived += OnMessage;
            _ws.ConnectionChanged += (ok, reason) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsConnected = ok;
                    Status = ok ? "Connected" : $"Disconnected: {reason}";
                    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
                });

                if (!ok && Settings.AutoReconnect)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(1500);
                            if (_cts == null || !_cts.IsCancellationRequested)
                                await StartAsync();
                        }
                        catch { }
                    });
                }
            };
        }
        private void AddDevToList(TokenItemViewModel? vm, bool toWhitelist)
        {
            try
            {
                if (vm == null)
                {
                    SimpleLog.Info("AddDevToList: null vm");
                    return;
                }

                var dev = vm.DevAddress?.Trim();
                if (string.IsNullOrWhiteSpace(dev))
                {
                    SimpleLog.Info("AddDevToList: empty dev");
                    return;
                }

                var (tried, used) = toWhitelist
                    ? DevListService.AddToWhitelist(dev)
                    : DevListService.AddToBlacklist(dev);

                if (toWhitelist) _devWhitelist.Add(dev);
                else _devBlacklist.Add(dev);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var t in Tokens.Where(t => string.Equals(t.DevAddress, dev, StringComparison.Ordinal)))
                        t.IsDevMarked = true;
                });

                SimpleLog.Info($"AddDevToList: {(toWhitelist ? "whitelist" : "blacklist")} +{dev} (used: {used})");
            }
            catch (Exception ex)
            {
                SimpleLog.Error($"AddDevToList error: {ex}");
            }
        }

        private void AddDevToList(string? dev, bool toWhitelist)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dev))
                {
                    SimpleLog.Info("AddDevToList: empty dev");
                    return;
                }

                var (pathTried, pathUsed) = toWhitelist
                    ? DevListService.AddToWhitelist(dev.Trim())
                    : DevListService.AddToBlacklist(dev.Trim());

                if (toWhitelist) _devWhitelist.Add(dev.Trim());
                else _devBlacklist.Add(dev.Trim());

                SimpleLog.Info($"AddDevToList: {(toWhitelist ? "whitelist" : "blacklist")} +{dev} (tried: {pathTried}, used: {pathUsed})");
            }
            catch (Exception ex)
            {
                SimpleLog.Error($"AddDevToList error: {ex}");
            }
        }

        private void OpenDevFile(string fileName)
        {
            try
            {
                var path = Path.Combine(DevListService.ExeDir, fileName);
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, "# one dev address per line\n");
                }
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SimpleLog.Error("OpenDevFile: " + ex.Message);
            }
        }

        private async Task StartAsync()
        {
            if (IsConnected) return;
            if (string.IsNullOrWhiteSpace(Settings.WebSocketEndpoint))
            {
                Status = "Введите Endpoint в Config";
                return;
            }
            if (string.IsNullOrWhiteSpace(Settings.ApiKey))
            {
                Status = "Введите API Key в Config";
                return;
            }


            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _startedAtUtc = DateTime.UtcNow;
            ParsedCount = 0; ErrorCount = 0;

            try
            {
                (_devWhitelist, _devBlacklist) = DevListService.Load(Settings.UseWhitelistFromFile, Settings.UseBlacklistFromFile);
                SimpleLog.Info($"Dev lists: whitelist={_devWhitelist.Count}, blacklist={_devBlacklist.Count}");

                await _ws.ConnectAsync(new Uri(Settings.WebSocketEndpoint), Settings.ApiKey, _cts.Token);
                _ = Task.Run(() => PingLoopAsync(_cts!.Token));

            }
            catch (Exception ex)
            {
                Status = "Connect error: " + ex.Message;
            }
        }
        private Uri? BuildPingUri()
        {
            try
            {
                var ws = new Uri(Settings.WebSocketEndpoint);
                var scheme = ws.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
                return new UriBuilder(scheme, ws.Host, ws.Port, "/ping").Uri;
            }
            catch
            {
                return null;
            }
        }

        private async Task PingLoopAsync(CancellationToken ct)
        {
            var pingUri = BuildPingUri();
            if (pingUri == null)
            {
                Application.Current.Dispatcher.Invoke(() => PingDisplay = "--");
                return;
            }

            // первый пинг сразу
            await DoPingOnceAsync(pingUri, ct);

            var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    await DoPingOnceAsync(pingUri, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // нормальное завершение
            }
        }

        private async Task DoPingOnceAsync(Uri pingUri, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, pingUri);

                // Если сервер требует ключ — передадим его:
                if (!string.IsNullOrWhiteSpace(Settings.ApiKey))
                    req.Headers.TryAddWithoutValidation("X-API-Key", Settings.ApiKey);

                var sw = Stopwatch.StartNew();
                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                sw.Stop();

                bool ok = resp.IsSuccessStatusCode;

                // Дополнительно: если пришёл JSON со status: "ok", считаем пинг успешным,
                // даже если код не 2xx (на случай 3xx/401 и т.п.)
                if (!ok)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("status", out var st) &&
                            st.ValueKind == JsonValueKind.String &&
                            string.Equals(st.GetString(), "ok", StringComparison.OrdinalIgnoreCase))
                        {
                            ok = true;
                        }
                    }
                    catch { /* игнор парс-ошибок */ }
                }

                var text = ok ? $"{sw.ElapsedMilliseconds} ms" : "fail";
                Application.Current.Dispatcher.Invoke(() => PingDisplay = text);
            }
            catch (TaskCanceledException)
            {
                // таймаут/стоп
                Application.Current.Dispatcher.Invoke(() => PingDisplay = "timeout");
            }
            catch (Exception ex)
            {
                SimpleLog.Info("Ping error: " + ex.Message);
                Application.Current.Dispatcher.Invoke(() => PingDisplay = "fail");
            }
        }


        private async Task StopAsync()
        {
            _cts?.Cancel();
            await _ws.DisconnectAsync();
            IsConnected = false;
            Status = "Disconnected by user";
            (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void OnMessage(string json)
        {
            RxCount++;
            LastJsonPreview = json.Length > 800 ? json[..800] + "...(cut)" : json;
            SimpleLog.Info("RX: " + LastJsonPreview);

            try
            {
                var msgs = TokenJsonParser.ExtractTokens(json);
                if (msgs.Count == 0)
                {
                    SimpleLog.Info("No tokens extracted from message");
                    return;
                }

                int added = 0;
                foreach (var m in msgs)
                {
                    if (Settings.IgnoreHistoryOnStart && m.created_at.HasValue)
                    {
                        var skew = TimeSpan.FromSeconds(Math.Max(0, Settings.TimeSkewSeconds));
                        if (m.created_at.Value.ToUniversalTime() < _startedAtUtc - skew)
                            continue;
                    }

                    if (!PassesFilters(m)) continue;

                    if (AddTokenIfPasses(m))
                    {
                        added++;

                        // 👇 авто-открыть в Axiom
                        if (Settings.AutoOpenInBrowser)
                        {
                            try
                            {
                                var url = BuildAxiomUrl(m);
                                if (!string.IsNullOrWhiteSpace(url))
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = url,
                                        UseShellExecute = true, // важно для открытия URL
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                SimpleLog.Error("AutoOpen failed: " + ex.Message);
                            }
                        }
                    }
                }
                ParsedCount += added;
            }
            catch (Exception ex)
            {
                ErrorCount++;
                SimpleLog.Info("Parse error: " + ex.Message);
            }
        }
        private static string BuildAxiomUrl(SolanaPumpTracker.Models.IncomingTokenMessage m)
        {
            // приоритет: pair_address -> bonding_curve -> mint
            var pool =
                !string.IsNullOrWhiteSpace(m.pair_address) ? m.pair_address :
                !string.IsNullOrWhiteSpace(m.bonding_curve) ? m.bonding_curve :
                m.mint;

            return string.IsNullOrWhiteSpace(pool) ? "" : $"https://axiom.trade/meme/{pool}";
        }

        private bool PassesFilters(IncomingTokenMessage m)
        {
            string dev = m.dev_info?.dev_address;
            if (string.IsNullOrWhiteSpace(dev))
                dev = m.creator; // fallback на creator, если dev_address отсутствует

            // Прецедент: whitelist имеет приоритет над blacklist
            if (Settings.UseWhitelistFromFile && _devWhitelist.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(dev) || !_devWhitelist.Contains(dev))
                    return Fail("dev not in whitelist_dev.txt");
            }

            if (Settings.UseBlacklistFromFile && _devBlacklist.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(dev) && _devBlacklist.Contains(dev))
                    return Fail("dev in blacklist_dev.txt");
            }

            // миграции
            var pct = m.dev_info?.migration_percentage ?? 0.0;
            if (pct < Settings.MinDevMigrationPct) return false;

            var migratedCount = m.dev_info?.migrated_tokens ?? 0;
            if (migratedCount < Settings.MinMigratedTokens) return false;

            if (Settings.RequireLastDevTokenMigrated)
            {
                IEnumerable<DevToken> list =
                    m.dev_info?.last_tokens
                    ?? m.dev_info?.recent_tokens
                    ?? m.dev_info?.last3_tokens
                    ?? m.dev_info?.dev_tokens
                    ?? Enumerable.Empty<DevToken>();

                var last = list
                    .OrderByDescending(dt => dt.created_at ?? DateTime.MinValue)
                    .FirstOrDefault();

                if (last == null || !last.migrated) return false;
            }


            if (Settings.PostOnly)
            {
                // считаем, что пост есть, если есть явная ссылка на твит ИЛИ известна дата твита
                bool hasTweet =
                    !string.IsNullOrWhiteSpace(m.twitter_info?.tweet_url) ||
                    m.twitter_info?.tweet_created_at is DateTime;

                if (!hasTweet) return false;
            }

            if ((m.token_info?.numHolders ?? 0) < Settings.MinNumHolders) return false;

            if ((m.token_info?.top10HoldersPercent ?? 0.0) > Settings.MaxTop10HoldersPct) return false;
            if ((m.token_info?.devHoldsPercent ?? 0.0) > Settings.MaxDevHoldsPct) return false;
            if ((m.token_info?.snipersHoldPercent ?? 0.0) > Settings.MaxSnipersHoldPct) return false;

            // twitter
            if ((m.twitter_info?.author_followers ?? 0) < Settings.MinAuthorFollowers) return false;
            if (Settings.MaxTweetAgeMinutes > 0 && m.twitter_info?.tweet_created_at is DateTime ttw)
            {
                var ageMin = (DateTime.UtcNow - ttw.ToUniversalTime()).TotalMinutes;
                if (ageMin > Settings.MaxTweetAgeMinutes) return false;
            }

            return true;
        }
        private static bool Fail(string why)
        {
            SimpleLog.Info("drop: " + why);
            return false;
        }


        private bool AddTokenIfPasses(IncomingTokenMessage m)
        {
            var vm = new TokenItemViewModel(m);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Tokens.Insert(0, vm); // сверху
                while (Tokens.Count > Settings.MaxItems) Tokens.RemoveAt(Tokens.Count - 1);
            });
            return true;
        }

        private void OpenConfig()
        {
            var win = new Views.ConfigWindow { Owner = Application.Current.MainWindow };
            win.LoadFrom(Settings);
            if (win.ShowDialog() == true)
            {
                Settings = win.SaveTo();
                SettingsService.Save(Settings);
            }
        }

        private void AddTestToken()
        {
            var m = new IncomingTokenMessage
            {
                mint = "TestMint" + DateTime.Now.Ticks,
                name = "Test Token",
                symbol = "TEST",
                sol_price = 160.825,
                created_at = DateTime.UtcNow,
                dev_info = new DevInfo { migration_percentage = 12.3, is_whitelisted = false },
                metadata = new Metadata { image = "https://via.placeholder.com/72x72.png?text=T", twitter = "https://x.com" },
                twitter_info = new TwitterInfo { author_username = "tester", author_followers = 1234, tweet_created_at = DateTime.UtcNow }
            };
            AddTokenIfPasses(m);
            ParsedCount++;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _ws.Dispose();
            _cts?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
