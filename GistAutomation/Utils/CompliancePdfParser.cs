using System.ClientModel;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using GistAutomation.Records;
using OpenAI.Chat;
using UglyToad.PdfPig;

namespace GistAutomation.Utils;

public static class CompliancePdfParser
{
  private const int BatchSize = 40;

  public static async Task<ComplianceInputFile> ParsePdfAsync(byte[] pdfBytes, string endpoint, string apiKey, string deployment)
  {
    // Step 1: Extract text from PDF locally
    Console.WriteLine("[AI] Extracting text from PDF...");
    var pdfText = ExtractTextFromPdf(pdfBytes);
    Console.WriteLine($"[AI] Extracted {pdfText.Length} characters from PDF.");

    // Step 2: First, get the list of all service names
    var clientOptions = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_04_01_Preview);
    var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey), clientOptions);
    var chatClient = client.GetChatClient(deployment);

    Console.WriteLine("[AI] Step 1: Getting list of all service names...");
    var serviceNames = await GetServiceNamesAsync(chatClient, pdfText);
    Console.WriteLine($"[AI] Found {serviceNames.Count} services. Processing in batches of {BatchSize}...");

    // Step 3: Process in batches
    var allEntries = new List<ComplianceInputEntry>();
    var batches = serviceNames.Chunk(BatchSize).ToList();

    for (int i = 0; i < batches.Count; i++)
    {
      var batch = batches[i];
      Console.WriteLine($"[AI] Processing batch {i + 1}/{batches.Count} ({batch.Length} services)...");
      var entries = await GetComplianceEntriesAsync(chatClient, pdfText, batch);
      allEntries.AddRange(entries);
      Console.WriteLine($"[AI] Batch {i + 1} returned {entries.Count} entries. Total so far: {allEntries.Count}");
    }

    if (allEntries.Count == 0)
      throw new InvalidOperationException("Azure OpenAI returned zero service entries from the PDF.");

    Console.WriteLine($"[AI] Extracted {allEntries.Count} service entries total.");

    return new ComplianceInputFile
    {
      SourceDocument = "Microsoft Azure Compliance Offerings",
      VerifiedDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
      Entries = allEntries
    };
  }

  private static async Task<List<string>> GetServiceNamesAsync(ChatClient chatClient, string pdfText)
  {
    var messages = new List<ChatMessage>
    {
      new SystemChatMessage("You extract Azure service names from compliance documents. Return ONLY a JSON array of service name strings exactly as they appear in the document."),
      new UserChatMessage($"List ALL Azure service names from both the Azure and Azure Government compliance tables in this document. Return a JSON array of unique service names.\n\n{pdfText}")
    };

    var options = new ChatCompletionOptions
    {
      ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
        jsonSchemaFormatName: "service_names",
        jsonSchema: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
            "serviceNames": {
              "type": "array",
              "items": { "type": "string" }
            }
          },
          "required": ["serviceNames"]
        }
        """),
        jsonSchemaIsStrict: false)
    };

    var response = await chatClient.CompleteChatAsync(messages, options);
    var content = response.Value.Content[0].Text;

    using var doc = JsonDocument.Parse(content);
    return doc.RootElement.GetProperty("serviceNames")
      .EnumerateArray()
      .Select(e => e.GetString()!)
      .Distinct()
      .ToList();
  }

  private static async Task<List<ComplianceInputEntry>> GetComplianceEntriesAsync(ChatClient chatClient, string pdfText, string[] serviceNames)
  {
    var serviceList = string.Join("\n", serviceNames.Select((s, i) => $"{i + 1}. {s}"));

    var messages = new List<ChatMessage>
    {
      new SystemChatMessage(BuildSystemPrompt()),
      new UserChatMessage(
        ChatMessageContentPart.CreateTextPart($"Here is the extracted text from the PDF:\n\n{pdfText}"),
        ChatMessageContentPart.CreateTextPart($"""
          Extract compliance data ONLY for these specific services:
          {serviceList}

          {BuildUserPrompt()}
          """))
    };

    var options = new ChatCompletionOptions
    {
      ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
        jsonSchemaFormatName: "compliance_input_file",
        jsonSchema: BinaryData.FromString(GetResponseSchema()),
        jsonSchemaIsStrict: false)
    };

    var response = await chatClient.CompleteChatAsync(messages, options);
    var content = response.Value.Content[0].Text;

    var inputFile = JsonSerializer.Deserialize<ComplianceInputFile>(content, new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    });

    return inputFile?.Entries ?? new List<ComplianceInputEntry>();
  }

  private static string ExtractTextFromPdf(byte[] pdfBytes)
  {
    var sb = new StringBuilder();
    using var document = PdfDocument.Open(pdfBytes);

    foreach (var page in document.GetPages())
    {
      sb.AppendLine($"--- Page {page.Number} ---");
      sb.AppendLine(page.Text);
      sb.AppendLine();
    }

    return sb.ToString();
  }

  private static string BuildSystemPrompt()
  {
    return """
            You are an Azure compliance data extraction specialist.
            You will be given a PDF document from Microsoft's Service Trust Portal
            that contains a compliance offerings matrix for Azure services.

            The document has a matrix showing Azure services as rows and compliance
            frameworks as columns. There are two separate tables: one for Azure
            (commercial) and one for Azure Government.

            The compliance frameworks (columns) are:
            1. CSA STAR Certification
            2. CSA STAR Attestation
            3. ISO 27001, 27018
            4. ISO 27017
            5. ISO 27701
            6. ISO 9001, 22301, 20000-1
            7. SOC 1, 2, 3
            8. GSMA SAS-SM
            9. HIPAA BAA
            10. HITRUST
            11. K-ISMS
            12. PCI 3DS
            13. PCI DSS
            14. Australia IRAP
            15. Germany C5
            16. Singapore MTCS Level 3
            17. Spain ENS High

            Rules:
            - Extract EVERY Azure service row from both the Azure and Azure Government tables.
            - For each service, set each framework flag to true if the cell contains a checkmark,
              "Yes", or similar positive indicator; set to false otherwise.
            - Use the exact service names as they appear in the document.
            - Set sourceDocument to the full document title as it appears in the PDF.
            - Set verifiedDate to today's date in ISO 8601 format.
            - Do NOT skip any service or framework. Be exhaustive.
            """;
  }

  private static string BuildUserPrompt()
  {
    return """
            Extract the full Azure compliance offerings matrix from this PDF.
            There are two tables: one for Azure (commercial) and one for Azure Government.
            
            For each Azure service, return its compliance status across all 17 frameworks
            for both Azure and Azure Government clouds.

            Return a JSON object with this structure:
            {
              "sourceDocument": "<full document title and version from the PDF>",
              "verifiedDate": "<today's date YYYY-MM-DD>",
              "entries": [
                {
                  "serviceName": "<Azure service name as listed>",
                  "azure": {
                    "csaStarCertification": true/false,
                    "csaStarAttestation": true/false,
                    "iso27001_27018": true/false,
                    "iso27017": true/false,
                    "iso27701": true/false,
                    "iso9001_22301_20000": true/false,
                    "soc1_2_3": true/false,
                    "gsmaSasSm": true/false,
                    "hipaaBaa": true/false,
                    "hitrust": true/false,
                    "kIsms": true/false,
                    "pci3ds": true/false,
                    "pciDss": true/false,
                    "australiaIrap": true/false,
                    "germanyC5": true/false,
                    "singaporeMtcsLevel3": true/false,
                    "spainEnsHigh": true/false
                  },
                  "azureGovernment": {
                    "csaStarCertification": true/false,
                    "csaStarAttestation": true/false,
                    "iso27001_27018": true/false,
                    "iso27017": true/false,
                    "iso27701": true/false,
                    "iso9001_22301_20000": true/false,
                    "soc1_2_3": true/false,
                    "gsmaSasSm": true/false,
                    "hipaaBaa": true/false,
                    "hitrust": true/false,
                    "kIsms": true/false,
                    "pci3ds": true/false,
                    "pciDss": true/false,
                    "australiaIrap": true/false,
                    "germanyC5": true/false,
                    "singaporeMtcsLevel3": true/false,
                    "spainEnsHigh": true/false
                  }
                }
              ]
            }

            If a service only appears in one table (e.g., only in Azure, not in Azure Government),
            set all flags for the missing cloud to false.
            """;
  }

  private static string GetResponseSchema()
  {
    var flagsSchema = """
            {
              "type": "object",
              "properties": {
                "csaStarCertification": { "type": "boolean" },
                "csaStarAttestation": { "type": "boolean" },
                "iso27001_27018": { "type": "boolean" },
                "iso27017": { "type": "boolean" },
                "iso27701": { "type": "boolean" },
                "iso9001_22301_20000": { "type": "boolean" },
                "soc1_2_3": { "type": "boolean" },
                "gsmaSasSm": { "type": "boolean" },
                "hipaaBaa": { "type": "boolean" },
                "hitrust": { "type": "boolean" },
                "kIsms": { "type": "boolean" },
                "pci3ds": { "type": "boolean" },
                "pciDss": { "type": "boolean" },
                "australiaIrap": { "type": "boolean" },
                "germanyC5": { "type": "boolean" },
                "singaporeMtcsLevel3": { "type": "boolean" },
                "spainEnsHigh": { "type": "boolean" }
              },
              "required": [
                "csaStarCertification", "csaStarAttestation",
                "iso27001_27018", "iso27017", "iso27701",
                "iso9001_22301_20000", "soc1_2_3", "gsmaSasSm",
                "hipaaBaa", "hitrust", "kIsms",
                "pci3ds", "pciDss", "australiaIrap",
                "germanyC5", "singaporeMtcsLevel3", "spainEnsHigh"
              ]
            }
            """;

    return $$"""
            {
              "type": "object",
              "properties": {
                "sourceDocument": { "type": "string" },
                "verifiedDate": { "type": "string" },
                "entries": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "serviceName": { "type": "string" },
                      "azure": {{flagsSchema}},
                      "azureGovernment": {{flagsSchema}}
                    },
                    "required": ["serviceName", "azure", "azureGovernment"]
                  }
                }
              },
              "required": ["sourceDocument", "verifiedDate", "entries"]
            }
            """;
  }
}
