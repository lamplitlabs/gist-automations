using System.Text.Json.Serialization;

namespace GistAutomation.Records;

public record ComplianceInputEntry
{
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = "";

    [JsonPropertyName("azure")]
    public ComplianceFrameworks Azure { get; set; } = new();

    [JsonPropertyName("azureGovernment")]
    public ComplianceFrameworks AzureGovernment { get; set; } = new();
}

public record ComplianceInputFile
{
    [JsonPropertyName("sourceDocument")]
    public string SourceDocument { get; set; } = "";

    [JsonPropertyName("verifiedDate")]
    public string VerifiedDate { get; set; } = "";

    [JsonPropertyName("entries")]
    public List<ComplianceInputEntry> Entries { get; set; } = new();
}