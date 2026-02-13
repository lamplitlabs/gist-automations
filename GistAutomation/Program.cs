using System.Text;
using System.Text.Json;
using GistAutomation.Records;
using GistAutomation.Utils;

namespace GistAutomation;

public class Program
{

    public static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: GistAutomation <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  sync-country      Sync country and state data to Gist");
            Console.WriteLine("  sync-currency     Sync currency data to Gist");
            Console.WriteLine("  sync-compliance   Download compliance PDF from STP, parse via Azure OpenAI, sync to Gist");
            return;
        }
        var command = args[0];

        switch (command)
        {
            case "sync-country":
                await SyncCountryAndState();
                break;
            case "sync-currency":
                await SyncCurrency();
                break;
            case "sync-compliance":
                await SyncCompliance();
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                break;
        }
    }

    public static async Task SyncCountryAndState()
    {
        var githubAccessToken = Environment.GetEnvironmentVariable("GIT_PERSONAL_TOKEN")
            ?? throw new Exception("GIT_PERSONAL_TOKEN is not set.");

        var gistId = Environment.GetEnvironmentVariable("COUNTRY_STATE_GIST_ID")
            ?? throw new Exception("COUNTRY_STATE_GIST_ID is not set.");

        var countries = await GeoName.GetCountries();
        var options = new JsonSerializerOptions { WriteIndented = true };
        var data = JsonSerializer.Serialize(countries, options);
        await GitHub.Save("country_state.json", githubAccessToken, gistId, data);
    }

    public static async Task SyncCurrency()
    {
        var githubAccessToken = Environment.GetEnvironmentVariable("GIT_PERSONAL_TOKEN")
            ?? throw new Exception("GIT_PERSONAL_TOKEN is not set.");

        var gistId = Environment.GetEnvironmentVariable("CURRENCY_GIST_ID")
            ?? throw new Exception("CURRENCY_GIST_ID is not set.");

        var countries = await GeoName.GetCountries();
        var currencyDataList = new List<CurrencyData>();
        foreach (var country in countries)
        {
            var currencyData = Currency.GetCurrency(country.Currency);
            currencyDataList.Add(new CurrencyData
            {
                Flag = country.Flag,
                CountryName = country.Name,
                Currency = currencyData?.CurrencyName ?? "",
                Code = currencyData?.CurrencyCode ?? "",
                Symbol = currencyData?.Symbol ?? "",
                SymbolImage = currencyData?.SymbolImage ?? ""
            });
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var data = JsonSerializer.Serialize(currencyDataList, options);
        await GitHub.Save("world_currency_symbols.json", githubAccessToken, gistId, data);
    }

    public static async Task SyncCompliance()
    {
        const string documentGuid = "7adf2d9e-d7b5-4e71-bad8-713e6a183cf3";
        const string gistId = "6c34ec94d1bf0d851e91f6be4abbc908";

        var githubAccessToken = Environment.GetEnvironmentVariable("GIT_PERSONAL_TOKEN")
            ?? throw new Exception("GIT_PERSONAL_TOKEN is not set.");
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new Exception("AZURE_OPENAI_ENDPOINT is not set.");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? throw new Exception("AZURE_OPENAI_API_KEY is not set.");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
            ?? throw new Exception("AZURE_OPENAI_DEPLOYMENT is not set.");

        Console.WriteLine("[Compliance] Downloading PDF from Service Trust Portal...");
        var stpResult = await StpDownloader.DownloadPdfAsync(documentGuid);

        var state = RunStateManager.Load();
        if (!string.IsNullOrEmpty(state.LastComplianceVersion) &&
            state.LastComplianceVersion == stpResult.WhenLastModified)
        {
            Console.WriteLine($"[Compliance] Document unchanged (whenLastModified: {stpResult.WhenLastModified}). Skipping.");
            return;
        }

        Console.WriteLine("[Compliance] Parsing PDF via Azure OpenAI...");
        var inputFile = await CompliancePdfParser.ParsePdfAsync(stpResult.PdfBytes, endpoint, apiKey, deployment);

        Console.WriteLine($"[Compliance] Extracted {inputFile.Entries.Count} entries from: {inputFile.SourceDocument}");

        var report = ComplianceReportBuilder.Build(inputFile);
        Console.WriteLine($"[Compliance] Built report with {report.Services.Count} normalized services.");

        var json = ComplianceReportBuilder.SerializeToJson(report);

        var syncDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        var description = $"Azure Services Compliance Coverage Matrix | " +
            $"Based on Microsoft's official compliance offerings report from the Service Trust Portal | " +
            $"Last synced: {syncDate}";
        await GitHub.Save("azure-compliance.json", githubAccessToken, gistId, json, description);

        RunStateManager.Save(new RunState
        {
            LastRun = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            LastComplianceVersion = stpResult.WhenLastModified
        });

        Console.WriteLine($"[Compliance] Successfully synced azure-compliance.json to Gist {gistId}.");
    }
}


