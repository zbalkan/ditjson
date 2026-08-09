using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ditjson.Models;

namespace ditjson.Output
{
    internal static class JsonOutputFormatter
    {
        public static string FormatStructuredOutput(List<User> users, List<Group> groups,
            List<Computer> computers)
        {
            var output = new
            {
                metadata = new
                {
                    exportDate = DateTime.UtcNow.ToString("O"),
                    ditjsonVersion = "1.0.2",
                    totalUsers = users.Count,
                    totalGroups = groups.Count,
                    totalComputers = computers.Count
                },
                users = users,
                groups = groups,
                computers = computers
            };

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(output, options);
        }
    }
}
