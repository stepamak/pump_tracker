using SolanaPumpTracker.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace SolanaPumpTracker.Utils
{
    public static class TokenJsonParser
    {
        // ---- Public API ------------------------------------------------------

        public static bool LooksLikeBatch(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                return root.ValueKind == JsonValueKind.Object
                       && root.TryGetProperty("tokens", out var arr)
                       && arr.ValueKind == JsonValueKind.Array;
            }
            catch { return false; }
        }

        public static bool IsInitialEnvelope(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return false;
                if (!root.TryGetProperty("type", out var t) || t.ValueKind != JsonValueKind.String) return false;
                return string.Equals(t.GetString(), "initial", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static List<IncomingTokenMessage> ExtractTokens(string json)
        {
            var list = new List<IncomingTokenMessage>();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("tokens", out var arr) &&
                arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                    TryAddToken(el, list);
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     root.TryGetProperty("token", out var one) &&
                     one.ValueKind == JsonValueKind.Object)
            {
                TryAddToken(one, list);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // иногда прилетает сразу плоский объект токена
                TryAddToken(root, list);
            }

            return list;
        }

        // ---- Implementation --------------------------------------------------

        private static void TryAddToken(JsonElement el, List<IncomingTokenMessage> sink)
        {
            try
            {
                var m = new IncomingTokenMessage
                {
                    mint = S(el, "mint"),
                    name = S(el, "name"),
                    symbol = S(el, "symbol"),
                    uri = S(el, "uri", "tokenUri"),
                    bonding_curve = S(el, "bonding_curve", "bondingCurve"),
                    pair_address = S(el, "pair_address", "pairAddress"),
                    creator = S(el, "creator", "deployerAddress"),
                    created_at = D(el, "created_at", "createdAt"),
                    sol_price = Db(el, "sol_price")
                };

                // dev_info
                if (Obj(el, out var dev, "dev_info"))
                {
                    var di = new DevInfo
                    {
                        dev_address = S(dev.Value, "dev_address", "creator", "deployerAddress"),
                        is_whitelisted = B(dev.Value, "is_whitelisted", "whitelisted"),
                        total_tokens = I(dev.Value, "total_tokens", "tokens_count", "tokenCount"),
                        migrated_tokens = I(dev.Value, "migrated_tokens", "migratedCount"),
                        migration_percentage = Db(dev.Value, "migration_percentage", "migrationPercent"),
                        dev_tokens = DevTokens(dev.Value, "dev_tokens"),
                        last_tokens = DevTokens(dev.Value, "last_tokens"),
                        recent_tokens = DevTokens(dev.Value, "recent_tokens"),
                        last3_tokens = DevTokens(dev.Value, "last3_tokens"),
                    };

                    if (di.migration_percentage <= 0 && di.total_tokens > 0)
                        di.migration_percentage = 100.0 * di.migrated_tokens / Math.Max(1, di.total_tokens);

                    m.dev_info = di;
                }

                // token_info
                if (Obj(el, out var ti, "token_info"))
                {
                    m.token_info = new TokenInfo
                    {
                        numHolders = I(ti.Value, "numHolders"),
                        numBotUsers = I(ti.Value, "numBotUsers"),
                        top10HoldersPercent = Db(ti.Value, "top10HoldersPercent"),
                        devHoldsPercent = Db(ti.Value, "devHoldsPercent"),
                        snipersHoldPercent = Db(ti.Value, "snipersHoldPercent"),
                        insidersHoldPercent = Db(ti.Value, "insidersHoldPercent"),
                        bundlersHoldPercent = Db(ti.Value, "bundlersHoldPercent"),
                        totalPairFeesPaid = Db(ti.Value, "totalPairFeesPaid"),
                        dexPaid = B(ti.Value, "dexPaid"),
                        dexPaidTime = D(ti.Value, "dexPaidTime")
                    };
                }

                // pair_info (нужен хотя бы pairAddress + возможные name/symbol fallback’и)
                if (Obj(el, out var pi, "pair_info"))
                {
                    // если в корне нет pair_address — вытащим из pair_info.pairAddress
                    if (string.IsNullOrWhiteSpace(m.pair_address))
                        m.pair_address = S(pi.Value, "pairAddress", "pair_address");

                    // name/symbol из pair_info, если корневые/metadata пустые
                    var pName = S(pi.Value, "tokenName");
                    var pSymbol = S(pi.Value, "tokenTicker");

                    // metadata fallback
                    if (m.metadata == null) m.metadata = new Metadata();
                    if (string.IsNullOrWhiteSpace(m.name) && string.IsNullOrWhiteSpace(m.metadata.name) && !string.IsNullOrWhiteSpace(pName))
                        m.metadata.name = pName;
                    if (string.IsNullOrWhiteSpace(m.symbol) && string.IsNullOrWhiteSpace(m.metadata.symbol) && !string.IsNullOrWhiteSpace(pSymbol))
                        m.metadata.symbol = pSymbol;
                }

                // metadata (если приходит отдельно)
                if (Obj(el, out var md, "metadata"))
                {
                    m.metadata = new Metadata
                    {
                        name = S(md.Value, "name") ?? m.metadata?.name ?? string.Empty,
                        symbol = S(md.Value, "symbol") ?? m.metadata?.symbol ?? string.Empty,
                        description = S(md.Value, "description") ?? string.Empty,
                        image = S(md.Value, "image") ?? m.metadata?.image ?? string.Empty,
                        twitter = S(md.Value, "twitter") ?? string.Empty,
                        website = S(md.Value, "website") ?? string.Empty,
                        show_name = B(md.Value, "show_name"),
                        created_on = S(md.Value, "created_on") ?? string.Empty
                    };
                }

                // twitter: сервер может слать twitter_info ИЛИ twitter_data
                if (Obj(el, out var tw, "twitter_info", "twitter_data"))
                {
                    m.twitter_info = new TwitterInfo
                    {
                        tweet_url = S(tw.Value, "tweet_url"),
                        community_url = S(tw.Value, "community_url"),
                        tweet_created_at = D(tw.Value, "tweet_created_at", "created_at"),
                        author_username = S(tw.Value, "author_username", "username"),
                        author_name = S(tw.Value, "author_name", "name"),
                        author_followers = L(tw.Value, "author_followers", "followers"),
                        is_blue_verified = B(tw.Value, "is_blue_verified", "blue_verified"),
                        verified_type = S(tw.Value, "verified_type"),
                        view_count = L(tw.Value, "view_count", "views"),
                        like_count = L(tw.Value, "like_count", "likes"),
                        retweet_count = L(tw.Value, "retweet_count", "retweets"),
                        reply_count = L(tw.Value, "reply_count", "replies"),
                        tweet_text = S(tw.Value, "tweet_text", "text")
                    };
                }

                // финальные фоллбеки имени/символа
                if (string.IsNullOrWhiteSpace(m.name))
                    m.name = m.metadata?.name ?? m.mint;
                if (string.IsNullOrWhiteSpace(m.symbol))
                    m.symbol = m.metadata?.symbol ?? "";

                sink.Add(m);
            }
            catch
            {
                // проглатываем один битый элемент, чтобы не ронять поток
            }
        }

        // ---- Helpers: tolerant getters with synonyms -------------------------

        private static bool Obj(JsonElement obj, out JsonElement? found, params string[] names)
        {
            found = null;
            foreach (var n in names)
            {
                if (obj.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Object)
                {
                    found = v;
                    return true;
                }
            }
            return false;
        }

        private static string? S(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            return null;
        }

        private static int I(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)) return i;
                    if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var si)) return si;
                }
            return 0;
        }

        private static long L(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var i)) return i;
                    if (v.ValueKind == JsonValueKind.String && long.TryParse(v.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var si)) return si;
                }
            return 0L;
        }

        private static double Db(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
                    if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var sd)) return sd;
                }
            return 0.0;
        }

        private static bool B(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var v))
                {
                    if (v.ValueKind == JsonValueKind.True) return true;
                    if (v.ValueKind == JsonValueKind.False) return false;
                    if (v.ValueKind == JsonValueKind.Number)
                    {
                        if (v.TryGetInt32(out var i)) return i != 0;
                        if (v.TryGetDouble(out var d)) return Math.Abs(d) > double.Epsilon;
                    }
                    if (v.ValueKind == JsonValueKind.String)
                    {
                        var s = v.GetString();
                        if (bool.TryParse(s, out var b)) return b;
                        if (int.TryParse(s, out var i2)) return i2 != 0;
                    }
                }
            return false;
        }

        private static DateTime? D(JsonElement obj, params string[] names)
        {
            foreach (var n in names)
                if (obj.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    var s = v.GetString();
                    if (!string.IsNullOrWhiteSpace(s) && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                        return dt;
                }
            return null;
        }

        private static List<DevToken>? DevTokens(JsonElement obj, string prop)
        {
            if (!obj.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array) return null;

            var list = new List<DevToken>();
            foreach (var it in arr.EnumerateArray())
            {
                if (it.ValueKind != JsonValueKind.Object) continue;
                list.Add(new DevToken
                {
                    created_at = D(it, "created_at", "createdAt"),
                    migrated = B(it, "migrated"),
                    pair_address = S(it, "pair_address", "pairAddress"),
                    token_address = S(it, "token_address", "tokenAddress", "mint"),
                    name = S(it, "name", "token_name"),
                    symbol = S(it, "symbol", "token_ticker")
                });
            }
            return list;
        }
    }
}
