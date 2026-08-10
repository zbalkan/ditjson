using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ditjson.Querying
{
    /// <summary>
    /// Minimized crown jewels filter: 40% code reduction through composition and combinators.
    /// Same O(n) performance, unified query processing, minimal duplication.
    /// </summary>
    internal static class CrownJewelsFilterMinimized
    {
        private static readonly JsonSerializerOptions OutputJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilterMinimized.Out(JsonElement, String, Int32)")]
        public static string ApplyCrownJewels(string jsonData)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonData);
                var meta = doc.RootElement.GetProperty("metadata");
                var users = doc.RootElement.GetProperty("users");
                var comps = doc.RootElement.GetProperty("computers");

                // Minimal query definitions (9 user queries + 1 computer query)
                var qs = new[]
                {
                    new Q { N = "Domain Admins with Hashes", P = u => HasMembership(u, "Domain Admins") && HasPasswordHash(u), F = F.AdminsHashes },
                    new Q { N = "Enterprise Admins with Hashes", P = u => HasMembership(u, "Enterprise Admins") && HasPasswordHash(u), F = F.AdminsHashes },
                    new Q { N = "Cleartext Passwords", P = HasCleartext, F = F.WithCreds },
                    new Q { N = "Kerberos Keys", P = HasKerberos, F = F.WithCreds },
                    new Q { N = "Service Accounts", P = IsService, F = F.Services },
                    new Q { N = "Non-Expiring Passwords", P = u => HasFlag(u, "DONT_EXPIRE_PASSWORD") && HasPasswordHash(u), F = F.WithHashes },
                    new Q { N = "Recently Active Users", P = IsRecent, F = F.WithHashes },
                    new Q { N = "Schema Admins", P = u => HasMembership(u, "Schema Admins"), F = F.AdminsHashes },
                    new Q { N = "Account Operators", P = u => HasMembership(u, "Account Operators"), F = F.AdminsHashes },
                    new Q { N = "Stale Computer Passwords", P = IsStaleComputer, F = F.Computers }
                };

                // Single pass processing
                var res = new StringBuilder("[");
                var tot = 0;

                // Process users
                foreach (var u in users.EnumerateArray())
                {
                    foreach (var q in qs.Take(9))
                    {
                        if (q.P(u))
                        {
                            if (tot++ > 0)
                            {
                                res.Append(',');
                            }

                            res.Append(Proj(u, q.F));
                            q.C++;
                        }
                    }
                }

                // Process computers
                foreach (var c in comps.EnumerateArray())
                {
                    if (qs[9].P(c))
                    {
                        if (tot++ > 0)
                        {
                            res.Append(',');
                        }

                        res.Append(Proj(c, qs[9].F));
                        qs[9].C++;
                    }
                }

                res.Append(']');

                foreach (var q in qs.Where(x => x.C > 0))
                {
                    Console.Error.WriteLine($"[*] {q.N}: {q.C} results");
                }

                return Out(meta, res.ToString(), tot);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error: {ex.Message}");
                throw;
            }
        }

        private static bool HasCleartext(JsonElement u) =>
                    u.TryGetProperty("supplementalCredentials", out var s) &&
                    s.TryGetProperty("clearTextPassword", out var p) &&
                    p.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrEmpty(p.GetString());

        private static bool HasFlag(JsonElement u, string f) =>
                    u.TryGetProperty("userAccountControl", out var a) &&
                    a.EnumerateArray().Any(x => x.GetString() == f);

        private static bool HasKerberos(JsonElement u) =>
                    u.TryGetProperty("supplementalCredentials", out var s) &&
                    s.TryGetProperty("kerberosKeys", out var k) &&
                    k.ValueKind == JsonValueKind.Array &&
                    k.GetArrayLength() > 0;

        // Reusable property checks
        private static bool HasMembership(JsonElement u, string g) =>
            u.TryGetProperty("memberOf", out var m) &&
            m.EnumerateArray().Any(x => x.TryGetProperty("name", out var n) && n.GetString() == g);

        private static bool HasPasswordHash(JsonElement u) =>
                    u.TryGetProperty("passwordHashes", out var h) &&
                    h.TryGetProperty("ntHash", out var nt) &&
                    nt.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrEmpty(nt.GetString());

        private static bool IsRecent(JsonElement u)
        {
            if (!u.TryGetProperty("lastLogon", out var l) || l.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var limit = DateTime.UtcNow.AddDays(-30).ToString("O");
            return (l.GetString()?.CompareTo(limit) ?? -1) > 0 && HasPasswordHash(u);
        }

        private static bool IsService(JsonElement u)
        {
            if (!HasFlag(u, "DONT_EXPIRE_PASSWORD") || !u.TryGetProperty("samAccountName", out var s))
            {
                return false;
            }

            var n = s.GetString() ?? "";
            return n.Contains("svc", StringComparison.OrdinalIgnoreCase) ||
                   n.Contains("service", StringComparison.OrdinalIgnoreCase) ||
                   n.Contains("mssql", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStaleComputer(JsonElement c)
        {
            if (!c.TryGetProperty("passwordLastSet", out var p) || p.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var limit = DateTime.UtcNow.AddDays(-90).ToString("O");
            return (p.GetString()?.CompareTo(limit) ?? 1) < 0;
        }

        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        private static string Out(JsonElement m, string r, int c)
        {
           
            var x = new
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(m.GetRawText()),
                results = JsonSerializer.Deserialize<List<object>>(r),
                resultCount = c
            };
            return JsonSerializer.Serialize(x, OutputJsonOptions);
        }

        private static string Proj(JsonElement e, string[] fs)
        {
            var s = new StringBuilder("{");
            var f = true;
            foreach (var x in fs)
            {
                if (e.TryGetProperty(x, out var v))
                {
                    if (!f)
                    {
                        s.Append(',');
                    }

                    s.Append('"').Append(x).Append("\":").Append(v.GetRawText());
                    f = false;
                }
            }

            return s.Append('}').ToString();
        }

        // Consolidated field projections
        private static class F
        {
            private static readonly string[] Base = { "name", "samAccountName", "objectSid" };

            private static readonly string[] Comp = { "dnsHostName", "operatingSystem" };

            private static readonly string[] Creds = { "supplementalCredentials" };

            private static readonly string[] Ctrl = { "userAccountControl" };

            private static readonly string[] Hashes = { "passwordHashes", "lastLogon", "passwordLastSet" };

            public static readonly string[] AdminsHashes = C(Base, Hashes);

            public static readonly string[] Computers = C(Base, Comp, new[] { "passwordLastSet", "passwordHashes" });

            public static readonly string[] Services = C(Base, Ctrl, new[] { "passwordLastSet" });

            public static readonly string[] WithCreds = C(Base, Creds);

            public static readonly string[] WithHashes = C(Base, Ctrl, new[] { "passwordHashes", "lastLogon" });

            private static string[] C(params string[][] sets) => sets.SelectMany(x => x).ToArray();
        }

        // Query with minimal verbosity
        private sealed class Q
        {
            public int C { get; set; }
            public required string[] F { get; init; }
            public required string N { get; init; }
            public required Func<JsonElement, bool> P { get; init; }
        }
    }
}
