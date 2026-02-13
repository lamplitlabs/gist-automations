using System.Text.Json;
using GistAutomation.Records;

namespace GistAutomation.Utils;

public static class ComplianceReportBuilder
{
    public static AzureComplianceReport Build(ComplianceInputFile inputFile)
    {
        var serviceMap = new Dictionary<string, AzureServiceCompliance>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in inputFile.Entries)
        {
            var canonicalName = AzureServiceNameNormalizer.Normalize(entry.ServiceName);

            if (serviceMap.ContainsKey(canonicalName))
            {
                Console.WriteLine(
                    $"[COMPLIANCE WARNING] Duplicate entry for '{canonicalName}' " +
                    $"(input: '{entry.ServiceName}'). Keeping first occurrence.");
                continue;
            }

            serviceMap[canonicalName] = new AzureServiceCompliance
            {
                ServiceName = canonicalName,
                Azure = entry.Azure,
                AzureGovernment = entry.AzureGovernment
            };
        }

        var sortedServices = serviceMap.Values
            .OrderBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AzureComplianceReport
        {
            SchemaVersion = "2.0",
            GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            SourceDescription = inputFile.SourceDocument,
            Services = sortedServices
        };
    }

    public static string SerializeToJson(AzureComplianceReport report)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(report, options);
    }
}
