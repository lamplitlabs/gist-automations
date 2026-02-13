using System.Net.Http.Headers;
using System.Text.Json;

namespace GistAutomation.Utils;

public record StpDownloadResult(byte[] PdfBytes, string WhenLastModified);

public static class StpDownloader
{
    private const string LiveDocBaseUrl = "https://api.servicetrust.microsoft.com/api/v2/GetLiveDocumentIncludingOldSeriesDoc";

    public static async Task<StpDownloadResult> DownloadPdfAsync(string documentGuid)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        var metadataUrl = $"{LiveDocBaseUrl}/{documentGuid}/";
        Console.WriteLine($"[STP] Fetching document metadata from {metadataUrl}...");

        var metaResponse = await client.GetAsync(metadataUrl);
        if (!metaResponse.IsSuccessStatusCode)
        {
            var body = await metaResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"STP metadata request failed with status {metaResponse.StatusCode}. Response: {body}");
        }

        var metaJson = await metaResponse.Content.ReadAsStringAsync();
        using var metaDoc = JsonDocument.Parse(metaJson);
        var docId = metaDoc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("STP metadata response missing 'id' field.");

        var whenLastModified = metaDoc.RootElement.TryGetProperty("whenLastModified", out var modProp)
            ? modProp.GetString() ?? ""
            : "";

        Console.WriteLine($"[STP] Resolved document id: {docId}, whenLastModified: {whenLastModified}");

        var downloadUrl = $"{LiveDocBaseUrl}/{documentGuid}/{docId}";
        Console.WriteLine($"[STP] Downloading PDF from {downloadUrl}...");

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        var dlResponse = await client.GetAsync(downloadUrl);

        if (!dlResponse.IsSuccessStatusCode)
        {
            var body = await dlResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"STP download failed with status {dlResponse.StatusCode}. Response: {body}");
        }

        var pdfBytes = await dlResponse.Content.ReadAsByteArrayAsync();
        Console.WriteLine($"[STP] Downloaded {pdfBytes.Length / 1024} KB.");

        return new StpDownloadResult(pdfBytes, whenLastModified);
    }
}
