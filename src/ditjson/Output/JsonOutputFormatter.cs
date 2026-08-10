using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ditjson.Models;

namespace ditjson.Output
{
    internal static class JsonOutputFormatter
    {
        private static readonly StructuredOutputJsonContext JsonContext = new(new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        public static string FormatStructuredOutput(List<User> users, List<Group> groups,
            List<Computer> computers, DatabaseFileMetadata? database = null)
        {
            var output = new StructuredOutput {
                Metadata = new ExportMetadata {
                    ExportDate = DateTime.UtcNow.ToString("O"),
                    DitjsonVersion = "1.0.2",
                    TotalUsers = users.Count,
                    TotalGroups = groups.Count,
                    TotalComputers = computers.Count,
                    Database = database
                },
                Users = users,
                Groups = groups,
                Computers = computers
            };

            return JsonSerializer.Serialize(output, JsonContext.StructuredOutput);
        }

        public static string FormatTimeline(List<User> users, List<Group> groups,
            List<Computer> computers)
        {
            var events = new List<TimelineEvent>();

            foreach (var item in users.Cast<NtdsObject>().Concat(groups).Concat(computers))
            {
                AddEvent(events, item.WhenCreated, "Created", item);
                AddEvent(events, item.WhenChanged, "Modified", item);
            }

            foreach (var user in users)
            {
                AddEvent(events, user.LastLogon, "Logged in", user);
                AddEvent(events, user.LastLogonTimeStamp, "Login timestamp sync", user);
                AddEvent(events, user.PasswordLastSet, "Password changed", user);
            }

            events.Sort((left, right) => StringComparer.Ordinal.Compare(left.Timestamp, right.Timestamp));
            return JsonSerializer.Serialize(events, JsonContext.ListTimelineEvent);
        }

        private static void AddEvent(List<TimelineEvent> events, string? timestamp, string action,
            NtdsObject item)
        {
            if (string.IsNullOrWhiteSpace(timestamp))
            {
                return;
            }

            events.Add(new TimelineEvent {
                Timestamp = timestamp,
                Event = action,
                RecordId = item.RecordId,
                ObjectName = item.Name,
                ObjectType = item.ObjectClass
            });
        }
    }

    internal sealed class ExportMetadata
    {
        [JsonPropertyName("database")]
        public DatabaseFileMetadata? Database { get; set; }

        [JsonPropertyName("ditjsonVersion")]
        public string DitjsonVersion { get; set; } = string.Empty;

        [JsonPropertyName("exportDate")]
        public string ExportDate { get; set; } = string.Empty;

        [JsonPropertyName("totalComputers")]
        public int TotalComputers { get; set; }

        [JsonPropertyName("totalGroups")]
        public int TotalGroups { get; set; }

        [JsonPropertyName("totalUsers")]
        public int TotalUsers { get; set; }
    }

    internal sealed class StructuredOutput
    {
        [JsonPropertyName("computers")]
        public List<Computer> Computers { get; set; } = new();

        [JsonPropertyName("groups")]
        public List<Group> Groups { get; set; } = new();

        [JsonPropertyName("metadata")]
        public ExportMetadata Metadata { get; set; } = new();

        [JsonPropertyName("users")]
        public List<User> Users { get; set; } = new();
    }

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(StructuredOutput))]
    // Keep credential nodes as explicit source-generation roots. Credential
    // values are populated after initial extraction, and these registrations
    // preserve their serialization contract in trimmed/single-file builds as
    // the surrounding model graph evolves.
    [JsonSerializable(typeof(User))]
    [JsonSerializable(typeof(Computer))]
    [JsonSerializable(typeof(DatabaseFileMetadata))]
    [JsonSerializable(typeof(PasswordHashes))]
    [JsonSerializable(typeof(SupplementalCredentials))]
    [JsonSerializable(typeof(KerberosKey))]
    [JsonSerializable(typeof(List<TimelineEvent>))]
    internal partial class StructuredOutputJsonContext : JsonSerializerContext
    {
    }

    internal sealed class TimelineEvent
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("objectName")]
        public string? ObjectName { get; set; }

        [JsonPropertyName("objectType")]
        public string? ObjectType { get; set; }

        [JsonPropertyName("recordId")]
        public int RecordId { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;
    }
}
