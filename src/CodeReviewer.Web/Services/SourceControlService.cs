using System.Net.Http.Headers;
using System.Text.Json;

namespace CodeReviewer.Web.Services;

public sealed record RepositoryItem(string Id, string Name, string Path, string WebUrl);
public sealed record MergeRequestItem(Guid ConnectionId, string RepositoryId, int Number, string Title, string Sha, string WebUrl, string Author,
    string Description = "", string SourceBranch = "", string TargetBranch = "", string State = "");
public sealed record ChangedFile(string Path, string Diff, bool Generated, bool TooLarge);

public interface ISourceControlProvider
{
    string Kind { get; }
    Task<string> PublishReviewAsync(SourceConnection connection, ReviewRun run, CancellationToken ct) => throw new NotSupportedException("انتشار برای این سورس کنترل هنوز پیاده‌سازی نشده است.");
    Task<IReadOnlyList<RepositoryItem>> GetRepositoriesAsync(SourceConnection connection, CancellationToken cancellationToken);
    Task<IReadOnlyList<MergeRequestItem>> GetOpenRequestsAsync(SourceConnection connection, string repositoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChangedFile>> GetChangesAsync(SourceConnection connection, string repositoryId, int number, CancellationToken cancellationToken);
    Task<string> TestAsync(SourceConnection connection, CancellationToken cancellationToken);
}

public sealed class SourceControlService(ConnectionCatalogStore store, IEnumerable<ISourceControlProvider> providers, SqlDataStore database)
{
    public bool IsConfigured => store.Snapshot().Sources.Any(x => x.Enabled);
    public IReadOnlyList<SourceConnection> Connections => store.Snapshot().Sources.Where(x => x.Enabled).ToArray();
    public Guid? DefaultConnectionId => store.Snapshot().DefaultSourceId;
    public Task<string> PublishReviewAsync(ReviewRun run, CancellationToken ct = default)
    {
        var (connection, provider) = Resolve(run.MergeRequest.ConnectionId);
        return provider.PublishReviewAsync(connection, run, ct);
    }

    public async Task<IReadOnlyList<RepositoryItem>> GetRepositoriesAsync(Guid id, CancellationToken ct = default)
    {
        var (connection, provider) = Resolve(id);
        var result = await provider.GetRepositoriesAsync(connection, ct);
        database.SaveSnapshot(id, "repositories", result);
        return result;
    }

    public async Task<IReadOnlyList<MergeRequestItem>> GetOpenRequestsAsync(Guid id, string repo, CancellationToken ct = default)
    {
        var (connection, provider) = Resolve(id);
        var result = await provider.GetOpenRequestsAsync(connection, repo, ct);
        database.SaveSnapshot(id, "requests:" + repo, result);
        return result;
    }

    public async Task<IReadOnlyList<ChangedFile>> GetChangesAsync(Guid id, string repo, int number, CancellationToken ct = default)
    {
        var (connection, provider) = Resolve(id);
        var result = await provider.GetChangesAsync(connection, repo, number, ct);
        database.SaveSnapshot(id, $"diff:{repo}:{number}", result);
        return result;
    }

    public Task<string> TestAsync(Guid id, CancellationToken ct = default)
    {
        var (connection, provider) = Resolve(id);
        return provider.TestAsync(connection, ct);
    }

    private (SourceConnection, ISourceControlProvider) Resolve(Guid id)
    {
        var connection = store.Snapshot().Sources.SingleOrDefault(x => x.Id == id && x.Enabled)
            ?? throw new InvalidOperationException("اتصال سورس کنترل فعال یافت نشد.");
        var provider = providers.SingleOrDefault(x => x.Kind == connection.Provider)
            ?? throw new NotSupportedException("آداپتور سورس کنترل یافت نشد.");
        return (connection, provider);
    }
}

public sealed partial class GitLabProvider(IHttpClientFactory clients) : ISourceControlProvider
{
    public string Kind => "GitLab";

    public async Task<IReadOnlyList<RepositoryItem>> GetRepositoriesAsync(SourceConnection connection, CancellationToken ct)
    {
        using var client = Client(connection);
        var items = await ListAll(client, "api/v4/projects?membership=true&simple=true&order_by=last_activity_at", ct);
        return items.Select(p => new RepositoryItem(
            p.GetProperty("id").GetInt32().ToString(), p.GetProperty("name").GetString() ?? "",
            p.GetProperty("path_with_namespace").GetString() ?? "", SourceControlUrls.GitLabWebUrl(connection,p.GetProperty("web_url").GetString() ?? ""))).ToArray();
    }

    public async Task<IReadOnlyList<MergeRequestItem>> GetOpenRequestsAsync(SourceConnection connection, string repo, CancellationToken ct)
    {
        using var client = Client(connection);
        var items = await ListAll(client, $"api/v4/projects/{Uri.EscapeDataString(repo)}/merge_requests?state=opened", ct);
        return items.Select(m => new MergeRequestItem(
            connection.Id, repo, m.GetProperty("iid").GetInt32(), m.GetProperty("title").GetString() ?? "",
            m.GetProperty("sha").GetString() ?? "", SourceControlUrls.GitLabWebUrl(connection,m.GetProperty("web_url").GetString() ?? ""),
            m.GetProperty("author").GetProperty("name").GetString() ?? "",
            m.GetProperty("description").GetString() ?? "", m.GetProperty("source_branch").GetString() ?? "",
            m.GetProperty("target_branch").GetString() ?? "", m.GetProperty("state").GetString() ?? "")).ToArray();
    }

    public async Task<IReadOnlyList<ChangedFile>> GetChangesAsync(SourceConnection connection, string repo, int number, CancellationToken ct)
    {
        using var client = Client(connection);
        var files = new List<ChangedFile>();
        for (var page = 1; true; page++)
        {
            using var json = await Get(client, $"api/v4/projects/{Uri.EscapeDataString(repo)}/merge_requests/{number}/diffs?per_page=100&page={page}", ct);
            var batch = json.RootElement.EnumerateArray().Select(d => new ChangedFile(
                d.GetProperty("new_path").GetString() ?? "", d.GetProperty("diff").GetString() ?? "",
                d.TryGetProperty("generated_file", out var generated) && generated.ValueKind == JsonValueKind.True,
                d.TryGetProperty("too_large", out var large) && large.ValueKind == JsonValueKind.True)).ToArray();
            files.AddRange(batch);
            if (batch.Length < 100) break;
        }
        return files;
    }

    public async Task<string> TestAsync(SourceConnection connection, CancellationToken ct)
    {
        using var client = Client(connection);
        using var json = await Get(client, "api/v4/version", ct);
        return $"GitLab {json.RootElement.GetProperty("version").GetString()}";
    }

    private HttpClient Client(SourceConnection connection)
    {
        var client = clients.CreateClient(ServiceHttp.SourceControlClientName);
        client.BaseAddress = new Uri(connection.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", connection.Token);
        return client;
    }

    internal static async Task<JsonDocument> Get(HttpClient client, string path, CancellationToken ct)
    {
        using var response = await client.GetAsync(path, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"سرویس پاسخ HTTP {(int)response.StatusCode} داد.");
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }
}

public sealed class GitHubProvider(IHttpClientFactory clients) : ISourceControlProvider
{
    public string Kind => "GitHub";

    public async Task<IReadOnlyList<RepositoryItem>> GetRepositoriesAsync(SourceConnection connection, CancellationToken ct)
    {
        using var client = Client(connection);
        var items = await GitLabProvider.ListAll(client, "user/repos?sort=pushed&affiliation=owner,collaborator,organization_member", ct);
        return items.Select(p => new RepositoryItem(
            p.GetProperty("full_name").GetString() ?? "", p.GetProperty("name").GetString() ?? "",
            p.GetProperty("full_name").GetString() ?? "", p.GetProperty("html_url").GetString() ?? "")).ToArray();
    }

    public async Task<IReadOnlyList<MergeRequestItem>> GetOpenRequestsAsync(SourceConnection connection, string repo, CancellationToken ct)
    {
        using var client = Client(connection);
        var items = await GitLabProvider.ListAll(client, $"repos/{repo}/pulls?state=open", ct);
        return items.Select(m => new MergeRequestItem(
            connection.Id, repo, m.GetProperty("number").GetInt32(), m.GetProperty("title").GetString() ?? "",
            m.GetProperty("head").GetProperty("sha").GetString() ?? "", m.GetProperty("html_url").GetString() ?? "",
            m.GetProperty("user").GetProperty("login").GetString() ?? "",
            m.GetProperty("body").GetString() ?? "", m.GetProperty("head").GetProperty("ref").GetString() ?? "",
            m.GetProperty("base").GetProperty("ref").GetString() ?? "", m.GetProperty("state").GetString() ?? "")).ToArray();
    }

    public async Task<IReadOnlyList<ChangedFile>> GetChangesAsync(SourceConnection connection, string repo, int number, CancellationToken ct)
    {
        using var client = Client(connection);
        var files = new List<ChangedFile>();
        for (var page = 1; true; page++)
        {
            using var json = await GitLabProvider.Get(client, $"repos/{repo}/pulls/{number}/files?per_page=100&page={page}", ct);
            var batch = json.RootElement.EnumerateArray().Select(f => new ChangedFile(
                f.GetProperty("filename").GetString() ?? "",
                f.TryGetProperty("patch", out var patch) ? patch.GetString() ?? "" : "",
                false, !f.TryGetProperty("patch", out _))).ToArray();
            files.AddRange(batch);
            if (batch.Length < 100) break;
        }
        return files;
    }

    public async Task<string> TestAsync(SourceConnection connection, CancellationToken ct)
    {
        using var client = Client(connection);
        using var json = await GitLabProvider.Get(client, "user", ct);
        return $"GitHub: {json.RootElement.GetProperty("login").GetString()}";
    }

    private HttpClient Client(SourceConnection connection)
    {
        var client = clients.CreateClient(ServiceHttp.SourceControlClientName);
        client.BaseAddress = new Uri(connection.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.Token);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CodeReviewer/0.1");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}


