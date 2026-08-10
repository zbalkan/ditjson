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
    /// Optimized crown jewels filter using single-pass algorithm and direct JSON generation.
    /// Time: O(n) single pass vs O(10n) multiple passes
    /// Memory: O(1) streaming vs O(r*m) result accumulation
    /// </summary>
    internal static class CrownJewelsFilterOptimized
    {
        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilterOptimized.BuildFinalOutput(JsonElement, String, Int32)")]
        public static string ApplyCrownJewels(string jsonData)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonData);
                var metadata = doc.RootElement.GetProperty("metadata");
                var users = doc.RootElement.GetProperty("users");
                var computers = doc.RootElement.GetProperty("computers");

                // Define all queries with their predicates - evaluated in single pass
                var allQueries = new[]
                {
                    new QueryContext { Name = "Domain Admins with Hashes", Predicate = u => HasMembership(u, "Domain Admins") && HasPasswordHash(u), Fields = FieldProjections.AdminsWithHashes },
                    new QueryContext { Name = "Enterprise Admins with Hashes", Predicate = u => HasMembership(u, "Enterprise Admins") && HasPasswordHash(u), Fields = FieldProjections.AdminsWithHashes },
                    new QueryContext { Name = "Cleartext Passwords", Predicate = HasCleartextPassword, Fields = FieldProjections.WithCredentials },
                    new QueryContext { Name = "Kerberos Keys", Predicate = HasKerberosKeys, Fields = FieldProjections.WithCredentials },
                    new QueryContext { Name = "Service Accounts", Predicate = IsServiceAccountWithNonExpiring, Fields = FieldProjections.ServiceAccounts },
                    new QueryContext { Name = "Non-Expiring Passwords", Predicate = u => HasFlag(u, "DONT_EXPIRE_PASSWORD") && HasPasswordHash(u), Fields = FieldProjections.WithHashes },
                    new QueryContext { Name = "Recently Active Users", Predicate = IsRecentlyActive, Fields = FieldProjections.WithHashes },
                    new QueryContext { Name = "Schema Admins", Predicate = u => HasMembership(u, "Schema Admins"), Fields = FieldProjections.AdminsWithHashes },
                    new QueryContext { Name = "Account Operators", Predicate = u => HasMembership(u, "Account Operators"), Fields = FieldProjections.AdminsWithHashes },
                    new QueryContext { Name = "Stale Computer Passwords", Predicate = IsStaleComputer, Fields = FieldProjections.Computers }
                };

                // Unified processing: single pass through all objects
                var results = new StringBuilder("[");
                var totalResults = 0;

                // Process users with first 9 queries
                foreach (var user in users.EnumerateArray())
                {
                    foreach (var query in allQueries.Take(9))
                    {
                        if (query.Predicate(user))
                        {
                            if (totalResults > 0)
                            {
                                results.Append(',');
                            }

                            results.Append(ProjectElementToJson(user, query.Fields));
                            query.Count++;
                            totalResults++;
                        }
                    }
                }

                // Process computers with 10th query
                var computerQuery = allQueries[9];
                foreach (var computer in computers.EnumerateArray())
                {
                    if (computerQuery.Predicate(computer))
                    {
                        if (totalResults > 0)
                        {
                            results.Append(',');
                        }

                        results.Append(ProjectElementToJson(computer, computerQuery.Fields));
                        computerQuery.Count++;
                        totalResults++;
                    }
                }

                results.Append(']');

                // Unified logging pass
                foreach (var query in allQueries)
                {
                    if (query.Count > 0)
                    {
                        Console.Error.WriteLine($"[*] {query.Name}: {query.Count} results");
                    }
                }

                return BuildFinalOutput(metadata, results.ToString(), totalResults);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error applying crown jewels: {ex.Message}");
                throw;
            }
        }

        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        private static string BuildFinalOutput(JsonElement metadata, string resultsJson, int totalCount)
        {
            var output = new
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata.GetRawText()),
                results = JsonSerializer.Deserialize<List<object>>(resultsJson),
                resultCount = totalCount
            };

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            return JsonSerializer.Serialize(output, options);
        }

        private static bool HasCleartextPassword(JsonElement user)
        {
            if (!user.TryGetProperty("supplementalCredentials", out var supp))
            {
                return false;
            }

            return supp.TryGetProperty("clearTextPassword", out var pwd) &&
                   pwd.ValueKind == JsonValueKind.String &&
                   !string.IsNullOrEmpty(pwd.GetString());
        }

        private static bool HasFlag(JsonElement user, string flag)
        {
            if (!user.TryGetProperty("userAccountControl", out var uac))
            {
                return false;
            }

            foreach (var item in uac.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && item.GetString() == flag)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasKerberosKeys(JsonElement user)
        {
            if (!user.TryGetProperty("supplementalCredentials", out var supp))
            {
                return false;
            }

            return supp.TryGetProperty("kerberosKeys", out var keys) &&
                   keys.ValueKind == JsonValueKind.Array &&
                   keys.GetArrayLength() > 0;
        }

        private static bool HasMembership(JsonElement user, string groupName)
        {
            if (!user.TryGetProperty("memberOf", out var memberOf))
            {
                return false;
            }

            foreach (var member in memberOf.EnumerateArray())
            {
                if (member.TryGetProperty("name", out var name) &&
                    name.GetString() == groupName)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasPasswordHash(JsonElement user)
        {
            if (!user.TryGetProperty("passwordHashes", out var hashes))
            {
                return false;
            }

            return hashes.TryGetProperty("ntHash", out var nt) &&
                   nt.ValueKind == JsonValueKind.String &&
                   !string.IsNullOrEmpty(nt.GetString());
        }

        private static bool IsRecentlyActive(JsonElement user)
        {
            if (!user.TryGetProperty("lastLogon", out var lastLogon) ||
                lastLogon.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).ToString("O");
            return lastLogon.GetString()?.CompareTo(thirtyDaysAgo) > 0 && HasPasswordHash(user);
        }

        private static bool IsServiceAccountWithNonExpiring(JsonElement user)
        {
            if (!HasFlag(user, "DONT_EXPIRE_PASSWORD"))
            {
                return false;
            }

            if (!user.TryGetProperty("samAccountName", out var sam) ||
                sam.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var samStr = sam.GetString() ?? "";
            return samStr.Contains("svc", StringComparison.OrdinalIgnoreCase) ||
                   samStr.Contains("service", StringComparison.OrdinalIgnoreCase) ||
                   samStr.Contains("mssql", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStaleComputer(JsonElement computer)
        {
            if (!computer.TryGetProperty("passwordLastSet", out var pwdSet) ||
                pwdSet.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90).ToString("O");
            return pwdSet.GetString()?.CompareTo(ninetyDaysAgo) < 0;
        }

        private static string ProjectElementToJson(JsonElement element, string[] fields)
        {
            var sb = new StringBuilder("{");
            var first = true;

            foreach (var field in fields)
            {
                if (element.TryGetProperty(field, out var value))
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    sb.Append('"').Append(field).Append("\":");
                    sb.Append(value.GetRawText());
                    first = false;
                }
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static class FieldProjections
        {
            public static readonly string[] AdminsWithHashes = Compose(Base, Hashes);

            public static readonly string[] Computers = Compose(Base, ComputerExtended, new[] { "passwordLastSet", "passwordHashes" });

            public static readonly string[] ServiceAccounts = Compose(Base, Uac, new[] { "passwordLastSet" });

            public static readonly string[] WithCredentials = Compose(Base, Credentials);

            public static readonly string[] WithHashes = Compose(Base, Uac, new[] { "passwordHashes", "lastLogon" });

            private static readonly string[] Base = { "name", "samAccountName", "objectSid" };

            private static readonly string[] ComputerExtended = { "dnsHostName", "operatingSystem" };

            private static readonly string[] Credentials = { "supplementalCredentials" };

            private static readonly string[] Hashes = { "passwordHashes", "lastLogon", "passwordLastSet" };

            private static readonly string[] Uac = { "userAccountControl" };

            private static string[] Compose(params string[][] sets) => sets.SelectMany(x => x).ToArray();
        }

        private sealed class QueryContext
        {
            public int Count { get; set; }
            public string[]? Fields { get; init; }
            public string? Name { get; init; }
            public Func<JsonElement, bool>? Predicate { get; init; }
        }
    }
}
