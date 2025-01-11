namespace CustomAvatarLoader.Versioning;

using CustomAvatarLoader.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

public class GitHubVersionChecker
{
    public GitHubVersionChecker(string repositoryName, ILogger logger)
    {
        RepositoryName = repositoryName;
        Logger = logger;
    }

    protected virtual string RepositoryName { get; }

    protected virtual ILogger Logger { get; }

    public virtual bool IsLatestVersionInstalled(string currentVersion)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                Logger.Info($"[VersionCheck] Current version: {currentVersion}");

                Logger.Info($"[VersionCheck] Checking for updates...");

                // GitHub API requires a user-agent
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("DesktopMate-Mod", currentVersion));

                // Fetch the tags from your GitHub repo
                string json = client.GetStringAsync(
                    $"https://api.github.com/repos/{RepositoryName}/tags").Result;

                // Deserialize the JSON array of tag objects
                List<GitHubTag> tags = JsonSerializer.Deserialize<List<GitHubTag>>(json);

                if (tags == null || tags.Count == 0)
                {
                    // For now just return true if we can't find any tags
                    return true;
                }

                // Find the latest version by comparing the numeric parts
                Version latestVersion = GetLatestVersion(tags);

                // Compare it to our local version
                var localVersion = new Version(currentVersion);

                return localVersion >= latestVersion;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("[VersionCheck] Error checking for updates", ex);

            return true;
        }
    }

    protected virtual Version GetLatestVersion(List<GitHubTag> tags)
    {
        Version highest = new Version(0, 0, 0);

        foreach (var tag in tags)
        {
            // Some tags start with 'v' (e.g. "v1.0.3"), strip that out
            string versionStr = tag.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? tag.Name.Substring(1)
                : tag.Name;

            if (Version.TryParse(versionStr, out Version parsed))
            {
                if (parsed > highest)
                    highest = parsed;
            }
        }

        return highest;
    }
}
