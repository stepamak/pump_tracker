using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using SolanaPumpTracker.Models;

namespace SolanaPumpTracker.Utils
{
    public static class TokenJsonParser
    {
        public static bool LooksLikeBatch(string json)
        {
            try
            {
                var root = JToken.Parse(json);
                return root is JObject o && o["tokens"] is JArray;
            }
            catch { return false; }
        }

        public static bool IsInitialEnvelope(string json)
        {
            try
            {
                var o = JObject.Parse(json);
                var t = (string?)o["type"];
                return string.Equals(t, "initial", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static List<IncomingTokenMessage> ExtractTokens(string json)
        {
            var result = new List<IncomingTokenMessage>();
            JToken root;
            try { root = JToken.Parse(json); } catch { return result; }

            if (root.Type == JTokenType.Object)
            {
                var obj = (JObject)root;

                if (obj.TryGetValue("tokens", out var tokensTok) && tokensTok is JArray tokensArr)
                {
                    foreach (var t in tokensArr.OfType<JObject>())
                    {
                        var msg = BuildIncoming(t);
                        if (msg != null) result.Add(msg);
                    }
                    return result;
                }

                if (obj.TryGetValue("token", out var tokenTok) && tokenTok is JObject tokenObj)
                {
                    var msg = BuildIncoming(tokenObj);
                    if (msg != null) result.Add(msg);
                    return result;
                }

                var one = BuildIncoming(obj);
                if (one != null) result.Add(one);
                return result;
            }

            if (root.Type == JTokenType.Array)
            {
                foreach (var t in ((JArray)root).OfType<JObject>())
                {
                    var msg = BuildIncoming(t);
                    if (msg != null) result.Add(msg);
                }
            }
            return result;
        }

        private static IncomingTokenMessage? BuildIncoming(JObject t)
        {
            string mint = S(t["mint"])
                       ?? S(t["token_mint"])
                       ?? S(t["tokenAddress"])
                       ?? S(t["token_address"])
                       ?? S(t["address"])
                       ?? S(t["pair_address"])
                       ?? S(t["bonding_curve"])
                       ?? string.Empty;
            if (string.IsNullOrWhiteSpace(mint)) return null;

            var metadataObj = t["metadata"] as JObject;

            string name = S(t["name"]) ?? S(metadataObj?["name"]) ?? mint;
            string symbol = S(t["symbol"]) ?? S(metadataObj?["symbol"]) ?? string.Empty;
            string image = S(metadataObj?["image"]) ?? S(t["image"]) ?? string.Empty;
            string twitter = S(metadataObj?["twitter"]) ?? S(t["twitter"]) ?? string.Empty;

            DateTime? createdAt = TryDate(S(t["created_at"]));

            var res = new IncomingTokenMessage
            {
                mint = mint,
                name = name,
                symbol = symbol,
                uri = S(t["uri"]) ?? string.Empty,
                bonding_curve = S(t["bonding_curve"]) ?? string.Empty,
                pair_address = S(t["pair_address"]) ?? string.Empty,
                creator = S(t["creator"]) ?? string.Empty,
                created_at = createdAt,
                metadata = new Metadata
                {
                    name = name,
                    symbol = symbol,
                    image = image,
                    twitter = twitter,
                    description = S(metadataObj?["description"]) ?? string.Empty,
                    website = S(metadataObj?["website"]) ?? string.Empty,
                    show_name = B(metadataObj?["show_name"]),
                    created_on = S(metadataObj?["created_on"]) ?? string.Empty
                },
                dev_info = new DevInfo(),
                token_info = new TokenInfo(),
                pair_info = new PairInfo(),
                twitter_info = new TwitterInfo(),
                sol_price = D(t["sol_price"])
            };

            // dev_info
            if (t["dev_info"] is JObject devObj)
            {
                res.dev_info.dev_address = S(devObj["dev_address"]) ?? string.Empty;
                res.dev_info.total_tokens = (int)D(devObj["total_tokens"]);
                res.dev_info.migrated_tokens = (int)D(devObj["migrated_tokens"]);
                res.dev_info.migration_percentage = devObj["migration_percentage"] != null
                    ? D(devObj["migration_percentage"])
                    : (res.dev_info.total_tokens > 0 ? (double)res.dev_info.migrated_tokens / res.dev_info.total_tokens * 100.0 : 0.0);
                res.dev_info.is_whitelisted = B(devObj["is_whitelisted"]);

                if (devObj["dev_tokens"] is JArray dt)
                {
                    foreach (var item in dt.OfType<JObject>())
                    {
                        res.dev_info.dev_tokens.Add(new DevToken
                        {
                            created_at = TryDate(S(item["created_at"])),
                            migrated = B(item["migrated"]),
                            token_address = S(item["token_address"]) ?? string.Empty,
                            token_name = S(item["token_name"]) ?? string.Empty
                        });
                    }
                }

                if (res.sol_price == 0.0 && devObj["dev_tokens"] is JArray dt2 && dt2.Count > 0)
                    res.sol_price = D(dt2[0]?["price_sol"]);
            }

            // pair_info
            if (t["pair_info"] is JObject p)
            {
                res.pair_info.initialLiquiditySol = D(p["initialLiquiditySol"]);
                res.pair_info.initialLiquidityToken = D(p["initialLiquidityToken"]);
            }

            // token_info
            if (t["token_info"] is JObject ti)
            {
                res.token_info.numHolders = (int)D(ti["numHolders"]);
                res.token_info.numBotUsers = (int)D(ti["numBotUsers"]);
                res.token_info.top10HoldersPercent = D(ti["top10HoldersPercent"]);
                res.token_info.devHoldsPercent = D(ti["devHoldsPercent"]);
                res.token_info.snipersHoldPercent = D(ti["snipersHoldPercent"]);
                res.token_info.insidersHoldPercent = D(ti["insidersHoldPercent"]);
                res.token_info.bundlersHoldPercent = D(ti["bundlersHoldPercent"]);
                res.token_info.totalPairFeesPaid = D(ti["totalPairFeesPaid"]);
                res.token_info.dexPaid = B(ti["dexPaid"]);
                res.token_info.dexPaidTime = TryDate(S(ti["dexPaidTime"]));
            }

            // twitter_data
            if (t["twitter_data"] is JObject tw)
            {
                var u = tw["userInfo"] as JObject;
                res.twitter_info.tweet_created_at = TryDate(S(tw["createdAt"]));
                res.twitter_info.view_count = (long)D(tw["viewCount"]);
                res.twitter_info.like_count = (long)D(tw["likeCount"]);
                res.twitter_info.retweet_count = (long)D(tw["retweetCount"]);
                res.twitter_info.reply_count = (long)D(tw["replyCount"]);
                res.twitter_info.tweet_url = S(tw["url"]) ?? S(tw["tweetUrl"]);
                if (tw["community"] is JObject c)
                    res.twitter_info.community_url = S(c["url"]);

                res.twitter_info.tweet_text =
                                                S(tw["text"]) ??
                                                S(tw["fullText"]) ??
                                                S(tw["content"]) ??
                                                S(tw["tweetText"]);
                var tweetId = S(tw["id"]) ?? S(tw["tweetId"]);
                if (string.IsNullOrWhiteSpace(res.twitter_info.tweet_url)
                    && !string.IsNullOrWhiteSpace(tweetId)
                    && !string.IsNullOrWhiteSpace(res.twitter_info.author_username))
                {
                    res.twitter_info.tweet_url = $"https://x.com/{res.twitter_info.author_username}/status/{tweetId}";
                }

                if (u != null)
                {
                    res.twitter_info.author_followers = (long)D(u["followers"]);
                    res.twitter_info.is_blue_verified = B(u["isBlueVerified"]);
                    res.twitter_info.verified_type = S(u["verifiedType"]);
                    res.twitter_info.author_name = S(u["name"]);
                    res.twitter_info.author_username = S(u["userName"]);
                }
            }

            return res;
        }

        private static string? S(JToken? tok)
            => tok == null || tok.Type == JTokenType.Null ? null :
               tok.Type == JTokenType.String ? (string)tok : tok.ToString();

        private static double D(JToken? tok)
        {
            if (tok == null || tok.Type == JTokenType.Null) return 0.0;
            var s = tok.Type == JTokenType.String ? (string)tok : tok.ToString();
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0.0;
        }

        private static bool B(JToken? tok)
        {
            if (tok == null || tok.Type == JTokenType.Null) return false;
            if (tok.Type == JTokenType.Boolean) return (bool)tok;
            var s = tok.ToString().Trim();
            return string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || s == "1";
        }

        private static DateTime? TryDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt)) return dt;
            if (DateTime.TryParse(s, out dt)) return dt;
            return null;
        }
    }
}
