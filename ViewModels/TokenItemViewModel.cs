using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SolanaPumpTracker.Models;

namespace SolanaPumpTracker.ViewModels
{
    public sealed class TokenItemViewModel : INotifyPropertyChanged
    {
        private readonly IncomingTokenMessage _m;

        public TokenItemViewModel(IncomingTokenMessage m)
        {
            _m = m;
        }

        public string Mint => _m.mint;
        public string Name => string.IsNullOrWhiteSpace(_m.name) ? _m.metadata?.name ?? _m.mint : _m.name;
        public string Symbol => string.IsNullOrWhiteSpace(_m.symbol) ? _m.metadata?.symbol ?? "" : _m.symbol;
        public string ImageUrl => _m.metadata?.image ?? "";
        public string Creator => _m.creator ?? "";
        public double SolPrice => _m.sol_price;
        public string CreatedAtDisplay => _m.created_at?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'") ?? "";

        // Dev
        public double DevMigrationPct => _m.dev_info?.migration_percentage ?? 0.0;
        public int DevMigratedCount => _m.dev_info?.migrated_tokens ?? 0;
        public int DevTotalCount => _m.dev_info?.total_tokens ?? 0;
        public bool IsWhitelisted => _m.dev_info?.is_whitelisted ?? false;

        // Token/Pair
        public double InitialLiqSol => _m.pair_info?.initialLiquiditySol ?? 0.0;
        public int NumHolders => _m.token_info?.numHolders ?? 0;
        public double Top10Pct => _m.token_info?.top10HoldersPercent ?? 0.0;
        public double DevHoldsPct => _m.token_info?.devHoldsPercent ?? 0.0;
        public double SnipersPct => _m.token_info?.snipersHoldPercent ?? 0.0;

        // Twitter
        public string AuthorUsername => _m.twitter_info?.author_username ?? "";
        public long AuthorFollowers => _m.twitter_info?.author_followers ?? 0;
        public string TweetCreatedAtDisplay => _m.twitter_info?.tweet_created_at?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'") ?? "N/A";
        public string TweetText => _m.twitter_info?.tweet_text ?? "";

        // Link
        public string PoolAddress =>
            !string.IsNullOrWhiteSpace(_m.pair_address) ? _m.pair_address :
            !string.IsNullOrWhiteSpace(_m.bonding_curve) ? _m.bonding_curve :
            _m.mint;

        public string LinkUrl => $"https://axiom.trade/meme/{PoolAddress}";


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
