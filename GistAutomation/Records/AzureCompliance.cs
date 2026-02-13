using System.Text.Json.Serialization;

namespace GistAutomation.Records;

public record ComplianceFrameworks
{
    [JsonPropertyName("csaStarCertification")]
    public bool CsaStarCertification { get; set; }

    [JsonPropertyName("csaStarAttestation")]
    public bool CsaStarAttestation { get; set; }

    [JsonPropertyName("iso27001_27018")]
    public bool Iso27001_27018 { get; set; }

    [JsonPropertyName("iso27017")]
    public bool Iso27017 { get; set; }

    [JsonPropertyName("iso27701")]
    public bool Iso27701 { get; set; }

    [JsonPropertyName("iso9001_22301_20000")]
    public bool Iso9001_22301_20000 { get; set; }

    [JsonPropertyName("soc1_2_3")]
    public bool Soc1_2_3 { get; set; }

    [JsonPropertyName("gsmaSasSm")]
    public bool GsmaSasSm { get; set; }

    [JsonPropertyName("hipaaBaa")]
    public bool HipaaBaa { get; set; }

    [JsonPropertyName("hitrust")]
    public bool Hitrust { get; set; }

    [JsonPropertyName("kIsms")]
    public bool KIsms { get; set; }

    [JsonPropertyName("pci3ds")]
    public bool Pci3ds { get; set; }

    [JsonPropertyName("pciDss")]
    public bool PciDss { get; set; }

    [JsonPropertyName("australiaIrap")]
    public bool AustraliaIrap { get; set; }

    [JsonPropertyName("germanyC5")]
    public bool GermanyC5 { get; set; }

    [JsonPropertyName("singaporeMtcsLevel3")]
    public bool SingaporeMtcsLevel3 { get; set; }

    [JsonPropertyName("spainEnsHigh")]
    public bool SpainEnsHigh { get; set; }
}

public record AzureServiceCompliance
{
    [JsonPropertyName("serviceName")]
    public string ServiceName { get; set; } = "";

    [JsonPropertyName("azure")]
    public ComplianceFrameworks Azure { get; set; } = new();

    [JsonPropertyName("azureGovernment")]
    public ComplianceFrameworks AzureGovernment { get; set; } = new();
}

public record AzureComplianceReport
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "2.0";

    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; set; } = "";

    [JsonPropertyName("sourceDescription")]
    public string SourceDescription { get; set; } = "";

    [JsonPropertyName("frameworks")]
    public List<string> Frameworks { get; set; } = new()
    {
        "CSA STAR Certification",
        "CSA STAR Attestation",
        "ISO 27001, 27018",
        "ISO 27017",
        "ISO 27701",
        "ISO 9001, 22301, 20000-1",
        "SOC 1, 2, 3",
        "GSMA SAS-SM",
        "HIPAA BAA",
        "HITRUST",
        "K-ISMS",
        "PCI 3DS",
        "PCI DSS",
        "Australia IRAP",
        "Germany C5",
        "Singapore MTCS Level 3",
        "Spain ENS High"
    };

    [JsonPropertyName("clouds")]
    public List<string> Clouds { get; set; } = new() { "Azure", "Azure Government" };

    [JsonPropertyName("disclaimer")]
    public string Disclaimer { get; set; } =
        "This data reflects Microsoft's platform-level compliance attestation scope as published " +
        "in the official audit reports obtained from the Service Trust Portal. " +
        "It does NOT constitute a compliance certification for any customer workload. " +
        "Customers must independently assess their own control implementations.";

    [JsonPropertyName("services")]
    public List<AzureServiceCompliance> Services { get; set; } = new();
}
