using System;
using System.Collections.Generic;

namespace SolanaPumpTracker.Models
{
    public sealed class IncomingTokenMessage
    {
        public string mint { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string symbol { get; set; } = string.Empty;
        public string uri { get; set; } = string.Empty;
        public string bonding_curve { get; set; } = string.Empty;
        public string pair_address { get; set; } = string.Empty;
        public string creator { get; set; } = string.Empty;
        public DateTime? created_at { get; set; }
        public DevInfo dev_info { get; set; } = new DevInfo();
        public TokenAnalysis token_analysis { get; set; } = new TokenAnalysis();
        public Metadata metadata { get; set; } = new Metadata();
        public TwitterInfo twitter_info { get; set; } = new TwitterInfo();
        public TokenInfo token_info { get; set; } = new TokenInfo();
        public PairInfo pair_info { get; set; } = new PairInfo();
        public double sol_price { get; set; }
    }

    public sealed class DevInfo
    {
        public string dev_address { get; set; } = string.Empty;
        public int total_tokens { get; set; }
        public int migrated_tokens { get; set; }
        public double migration_percentage { get; set; }
        public bool is_whitelisted { get; set; }
        public List<DevToken> dev_tokens { get; set; } = new();
    }

    public sealed class DevToken
    {
        public DateTime? created_at { get; set; }
        public bool migrated { get; set; }
        public string token_address { get; set; } = string.Empty;
        public string token_name { get; set; } = string.Empty;
    }

    public sealed class TokenAnalysis
    {
        public string creator_risk_level { get; set; } = string.Empty;
        public int creator_rug_count { get; set; }
        public int creator_token_count { get; set; }
    }

    public sealed class Metadata
    {
        public string name { get; set; } = string.Empty;
        public string symbol { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string image { get; set; } = string.Empty;
        public string twitter { get; set; } = string.Empty;
        public string website { get; set; } = string.Empty;
        public bool show_name { get; set; }
        public string created_on { get; set; } = string.Empty;
    }

    public sealed class TwitterInfo
    {
        public DateTime? tweet_created_at { get; set; }
        public long author_followers { get; set; }
        public bool is_blue_verified { get; set; }
        public string? verified_type { get; set; }
        public string? author_name { get; set; }
        public string? author_username { get; set; }
        public long view_count { get; set; }
        public long like_count { get; set; }
        public long retweet_count { get; set; }
        public long reply_count { get; set; }
        public string? tweet_url { get; set; }
        public string? community_url { get; set; }
        public string? tweet_text { get; set; }


    }

    public sealed class TokenInfo
    {
        public int numHolders { get; set; }
        public int numBotUsers { get; set; }
        public double top10HoldersPercent { get; set; }
        public double devHoldsPercent { get; set; }
        public double snipersHoldPercent { get; set; }
        public double insidersHoldPercent { get; set; }
        public double bundlersHoldPercent { get; set; }
        public double totalPairFeesPaid { get; set; }
        public bool dexPaid { get; set; }
        public DateTime? dexPaidTime { get; set; }
    }

    public sealed class PairInfo
    {
        public double initialLiquiditySol { get; set; }
        public double initialLiquidityToken { get; set; }
    }
}
