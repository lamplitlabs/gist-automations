using Octokit;
using Octokit.Internal;

namespace GistAutomation.Utils;

public static class GitHub
{
    private const string GIST_UPDATE_HEADER = "gistupdate";

    public static async Task Save(string fileName, string githubAccessToken, string gistId, string data, string? description = null)
    {
        var credentials = new InMemoryCredentialStore(new Credentials(githubAccessToken));
        var client = new GitHubClient(new ProductHeaderValue(GIST_UPDATE_HEADER), credentials);
        var gistUpdate = new GistUpdate
        {
            Description = description ?? $"List Of Countries With States And Other Useful Information, Updated On {DateTime.UtcNow}",
        };
        gistUpdate.Files.Add(fileName, new GistFileUpdate
        {
            Content = data
        });
        await client.Gist.Edit(gistId, gistUpdate);
    }
}