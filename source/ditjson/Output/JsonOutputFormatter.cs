using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ditjson.Models;

namespace ditjson.Output
{
    internal static class JsonOutputFormatter
    {
        private static readonly StructuredOutputJsonContext JsonContext = new(new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        public static string FormatStructuredOutput(List<User> users, List<Group> groups,
            List<Computer> computers)
        {
            var output = new StructuredOutput
            {
                Metadata = new ExportMetadata
                {
                    ExportDate = DateTime.UtcNow.ToString("O"),
                    DitjsonVersion = "1.0.2",
                    TotalUsers = users.Count,
                    TotalGroups = groups.Count,
                    TotalComputers = computers.Count
                },
                Users = users,
                Groups = groups,
                Computers = computers
            };

            return JsonSerializer.Serialize(output, JsonContext.StructuredOutput);
        }
    }

    internal sealed class StructuredOutput
    {
        [JsonPropertyName("metadata")]
        public ExportMetadata Metadata { get; set; } = new();

        [JsonPropertyName("users")]
        public List<User> Users { get; set; } = new();

        [JsonPropertyName("groups")]
        public List<Group> Groups { get; set; } = new();

        [JsonPropertyName("computers")]
        public List<Computer> Computers { get; set; } = new();
    }

    internal sealed class ExportMetadata
    {
        [JsonPropertyName("exportDate")]
        public string ExportDate { get; set; } = string.Empty;

        [JsonPropertyName("ditjsonVersion")]
        public string DitjsonVersion { get; set; } = string.Empty;

        [JsonPropertyName("totalUsers")]
        public int TotalUsers { get; set; }

        [JsonPropertyName("totalGroups")]
        public int TotalGroups { get; set; }

        [JsonPropertyName("totalComputers")]
        public int TotalComputers { get; set; }
    }

    [JsonSourceGenerationOptions(
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        UseStringEnumConverter = true)]
    [JsonSerializable(typeof(StructuredOutput))]
    internal partial class StructuredOutputJsonContext : JsonSerializerContext
    {
    }
}
