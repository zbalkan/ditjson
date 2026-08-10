using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ditjson.Querying
{
    /// <summary>
    /// Extracts operationally valuable "crown jewels" from NTDS dumps using hardcoded C# filtering.
    /// Default behavior reduces 250k user dumps to ~50-100 results (90%+ size reduction).
    /// </summary>
    internal static class CrownJewelsFilter
    {
        private static readonly JsonSerializerOptions OutputJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.BuildOutputJson(JsonElement, List<JsonElement>)")]
        public static string ApplyCrownJewels(string jsonData)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonData);
                var metadata = doc.RootElement.GetProperty("metadata");
                var users = doc.RootElement.GetProperty("users");
                var computers = doc.RootElement.GetProperty("computers");

                var results = new List<JsonElement>();

                // Execute all crown jewel queries
                results.AddRange(QueryByGroup(users, "Domain Admins", HasPasswordHash, FieldProjections.AdminsWithHashes, "[*] Domain Admins with Hashes"));
                results.AddRange(QueryByGroup(users, "Enterprise Admins", HasPasswordHash, FieldProjections.AdminsWithHashes, "[*] Enterprise Admins with Hashes"));
                results.AddRange(QueryByCleartext(users, "[*] Cleartext Passwords"));
                results.AddRange(QueryByKerberos(users, "[*] Kerberos Keys"));
                results.AddRange(QueryServiceAccounts(users, "[*] Service Accounts"));
                results.AddRange(QueryByFlag(users, "DONT_EXPIRE_PASSWORD", HasPasswordHash, FieldProjections.WithHashes, "[*] Non-Expiring Passwords"));
                results.AddRange(QueryRecentlyActive(users, "[*] Recently Active Users"));
                results.AddRange(QueryStaleComputers(computers, "[*] Stale Computer Passwords"));
                results.AddRange(QueryByGroup(users, "Schema Admins", _ => true, FieldProjections.AdminsWithHashes, "[*] Schema Admins"));
                results.AddRange(QueryByGroup(users, "Account Operators", _ => true, FieldProjections.AdminsWithHashes, "[*] Account Operators"));

                return BuildOutputJson(metadata, results);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[!] Error applying crown jewels: {ex.Message}");
                throw;
            }
        }

        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        private static string BuildOutputJson(JsonElement metadata, List<JsonElement> results)
        {
            var output = new
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata.GetRawText()),
                results = results.Select(r => JsonSerializer.Deserialize<object>(r.GetRawText())).ToList(),
                resultCount = results.Count
            };

            return JsonSerializer.Serialize(output, OutputJsonOptions);
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

            if (hashes.TryGetProperty("ntHash", out var nt) &&
                nt.ValueKind == JsonValueKind.String &&
                !string.IsNullOrEmpty(nt.GetString()))
            {
                return true;
            }

            return false;
        }

        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        private static JsonElement ProjectElement(JsonElement element, string[] fields)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var field in fields)
            {
                if (element.TryGetProperty(field, out var value))
                {
                    dict[field] = JsonSerializer.Deserialize<object>(value.GetRawText());
                }
            }

            var json = JsonSerializer.Serialize(dict);
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.ProjectElement(JsonElement, String[])")]
        private static List<JsonElement> QueryByCleartext(JsonElement users, string logMessage)
        {
            Console.Error.WriteLine(logMessage);
            var results = new List<JsonElement>();

            foreach (var user in users.EnumerateArray())
            {
                if (user.TryGetProperty("supplementalCredentials", out var supp) &&
                    supp.TryGetProperty("clearTextPassword", out var pwd) &&
                    pwd.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrEmpty(pwd.GetString()))
                {
                    results.Add(ProjectElement(user, FieldProjections.WithCredentials));
                }
            }

            if (results.Count > 0)
            {
                Console.Error.WriteLine($"[+] Found {results.Count} results");
            }

            return results;
        }

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.ProjectElement(JsonElement, String[])")]
        private static List<JsonElement> QueryByFlag(JsonElement users, string flag, Func<JsonElement, bool> additionalFilter, string[] fields, string logMessage)
        {
            Console.Error.WriteLine(logMessage);
            var results = new List<JsonElement>();

            foreach (var user in users.EnumerateArray())
            {
                if (HasFlag(user, flag) && additionalFilter(user))
                {
                    results.Add(ProjectElement(user, fields));
                }
            }

            if (results.Count > 0)
            {
                Console.Error.WriteLine($"[+] Found {results.Count} results");
            }

            return results;
        }

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.ProjectElement(JsonElement, String[])")]
        private static List<JsonElement> QueryByGroup(JsonElement users, string groupName, Func<JsonElement, bool> additionalFilter, string[] fields, string logMessage)
        {
            Console.Error.WriteLine(logMessage);
            var results = new List<JsonElement>();

            foreach (var user in users.EnumerateArray())
            {
                if (HasMembership(user, groupName) && additionalFilter(user))
                {
                    results.Add(ProjectElement(user, fields));
                }
            }

            if (results.Count > 0)
            {
                Console.Error.WriteLine($"[+] Found {results.Count} results");
            }

            return results;
        }

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.ProjectElement(JsonElement, String[])")]
        private static List<JsonElement> QueryByKerberos(JsonElement users, string logMessage)
        {
            Console.Error.WriteLine(logMessage);
            var results = new List<JsonElement>();

            foreach (var user in users.EnumerateArray())
            {
                if (user.TryGetProperty("supplementalCredentials", out var supp) &&
                    supp.TryGetProperty("kerberosKeys", out var keys) &&
                    keys.ValueKind == JsonValueKind.Array &&
                    keys.GetArrayLength() > 0)
                {
                    results.Add(ProjectElement(user, FieldProjections.WithCredentials));
                }
            }

            if (results.Count > 0)
            {
                Console.Error.WriteLine($"[+] Found {results.Count} results");
            }

            return results;
        }

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.ProjectElement(JsonElement, String[])")]
        private static List<JsonElement> QueryRecentlyActive(JsonElement users, string logMessage)
        {
            Console.Error.WriteLine(logMessage);
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).ToString("O");
            var results = new List<JsonElement>();

            foreach (var user in users.EnumerateArray())
            {
                if (user.TryGetProperty("lastLogon", out var lastLogon) &&
                    lastLogon.ValueKind == JsonValueKind.String &&
                    lastLogon.GetString()?.CompareTo(thirtyDaysAgo) > 0 &&
                    HasPasswordHash(user))
                {
                    results.Add(ProjectElement(user, FieldProjections.WithHashes));
                }
            }

            if (results.Count > 0)
            {
                Console.Error.WriteLine($"[+] Found {results.Count} results");
            }

            return results;
        }

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.ProjectElement(JsonElement, String[])")]
        private static List<JsonElement> QueryServiceAccounts(JsonElement users, string logMessage)
        {
            Console.Error.WriteLine(logMessage);
            var results = new List<JsonElement>();
            var patterns = new[] { "svc", "service", "mssql" };

            foreach (var user in users.EnumerateArray())
            {
                if (HasFlag(user, "DONT_EXPIRE_PASSWORD") &&
                    user.TryGetProperty("samAccountName", out var sam) &&
                    sam.ValueKind == JsonValueKind.String)
                {
                    var samStr = sam.GetString() ?? "";
                    if (patterns.Any(p => samStr.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(ProjectElement(user, FieldProjections.ServiceAccounts));
                    }
                }
            }

            if (results.Count > 0)
            {
                Console.Error.WriteLine($"[+] Found {results.Count} results");
            }

            return results;
        }

        [RequiresUnreferencedCode("Calls ditjson.Querying.CrownJewelsFilter.ProjectElement(JsonElement, String[])")]
        private static List<JsonElement> QueryStaleComputers(JsonElement computers, string logMessage)
        {
            Console.Error.WriteLine(logMessage);
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90).ToString("O");
            var results = new List<JsonElement>();

            foreach (var computer in computers.EnumerateArray())
            {
                if (computer.TryGetProperty("passwordLastSet", out var pwdSet) &&
                    pwdSet.ValueKind == JsonValueKind.String &&
                    pwdSet.GetString()?.CompareTo(ninetyDaysAgo) < 0)
                {
                    results.Add(ProjectElement(computer, FieldProjections.Computers));
                }
            }

            if (results.Count > 0)
            {
                Console.Error.WriteLine($"[+] Found {results.Count} results");
            }

            return results;
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

            private static string[] Compose(params string[]?[]? sets)
            {
                if (sets == null || sets.Length == 0)
                {
                    return Array.Empty<string>();
                }
                
                return sets.Where(x => x != null).SelectMany(x => x!).ToArray();
            }
        }
    }
}
