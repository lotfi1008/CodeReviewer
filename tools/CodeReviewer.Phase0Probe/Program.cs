using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI;

var mode = args.Length == 0 ? "all" : args[0].ToLowerInvariant();
if (mode is not ("all" or "gitlab" or "model"))
{
    Console.Error.WriteLine("Usage: dotnet run -- [all|gitlab|model]");
    return 2;
}

var failed = false;
if (mode is "all" or "gitlab")
    failed |= !await CheckGitLabAsync();
if (mode is "all" or "model")
    failed |= !await CheckModelAsync();
return failed ? 1 : 0;

static async Task<bool> CheckGitLabAsync()
{
    var baseUrl = Environment.GetEnvironmentVariable("REVIEW_GITLAB_URL");
    var token = Environment.GetEnvironmentVariable("REVIEW_GITLAB_TOKEN");
    var project = Environment.GetEnvironmentVariable("REVIEW_GITLAB_PROJECT");
    var iid = Environment.GetEnvironmentVariable("REVIEW_GITLAB_MR_IID");
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
        string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(project) ||
        !int.TryParse(iid, out var mrIid) || mrIid < 1)
    {
        Console.Error.WriteLine("GitLab: set REVIEW_GITLAB_URL (HTTPS), REVIEW_GITLAB_TOKEN, REVIEW_GITLAB_PROJECT and REVIEW_GITLAB_MR_IID.");
        return false;
    }

    using var client = new HttpClient(CreateHandler()) { BaseAddress = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(20) };
    client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    var mrPath = $"api/v4/projects/{Uri.EscapeDataString(project)}/merge_requests/{mrIid}";
    try
    {
        using var version = await client.GetAsync("api/v4/version");
        if (!version.IsSuccessStatusCode)
            return Fail("GitLab version", version.StatusCode);
        using var versionJson = JsonDocument.Parse(await version.Content.ReadAsStreamAsync());
        var versionText = versionJson.RootElement.GetProperty("version").GetString();
        Console.WriteLine($"GitLab version: {versionText}");

        using var mr = await client.GetAsync(mrPath);
        if (!mr.IsSuccessStatusCode)
            return Fail("GitLab MR", mr.StatusCode);
        using var mrJson = JsonDocument.Parse(await mr.Content.ReadAsStreamAsync());
        var sha = mrJson.RootElement.GetProperty("sha").GetString();
        if (string.IsNullOrWhiteSpace(sha))
            throw new JsonException("MR head SHA is missing.");

        using var diffs = await client.GetAsync(mrPath + "/diffs?per_page=1");
        if (!diffs.IsSuccessStatusCode)
            return Fail("GitLab diffs", diffs.StatusCode);
        using var diffJson = JsonDocument.Parse(await diffs.Content.ReadAsStreamAsync());
        if (diffJson.RootElement.ValueKind != JsonValueKind.Array || diffJson.RootElement.GetArrayLength() == 0)
            throw new JsonException("The test MR has no accessible diff files.");
        Console.WriteLine($"GitLab MR: OK; head SHA {sha}; first diff page accessible.");
        return true;
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
    {
        Console.Error.WriteLine($"GitLab: failed ({ex.GetType().Name}). Check connectivity, token permissions, MR and API compatibility.");
        return false;
    }
}

static async Task<bool> CheckModelAsync()
{
    var endpoint = Environment.GetEnvironmentVariable("REVIEW_MODEL_ENDPOINT");
    var key = Environment.GetEnvironmentVariable("REVIEW_MODEL_API_KEY");
    var model = Environment.GetEnvironmentVariable("REVIEW_MODEL_NAME");
    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
        string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
    {
        Console.Error.WriteLine("Model: set REVIEW_MODEL_ENDPOINT (HTTPS, OpenAI-compatible base URL), REVIEW_MODEL_API_KEY and REVIEW_MODEL_NAME.");
        return false;
    }

    try
    {
        using var http = new HttpClient(CreateHandler());
        var options = new OpenAIClientOptions { Endpoint = uri, NetworkTimeout = TimeSpan.FromSeconds(60), Transport = new HttpClientPipelineTransport(http) };
        using IChatClient chat = new OpenAIClient(new ApiKeyCredential(key), options)
            .GetChatClient(model).AsIChatClient();
        var response = await chat.GetResponseAsync<ProbeFinding>(
            "Return one JSON finding for this C# code: int Divide(int a, int b) => a / b; " +
            "Use category 'correctness', a short title, and confidence between 0 and 1.",
            useJsonSchemaResponseFormat: true);
        var finding = response.Result;
        if (finding is null || finding.Category != "correctness" ||
            string.IsNullOrWhiteSpace(finding.Title) || finding.Confidence is < 0 or > 1)
            throw new JsonException("The model returned an invalid structured finding.");
        Console.WriteLine($"Model: IChatClient structured output OK ({model}).");
        return true;
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or ClientResultException or NotSupportedException)
    {
        Console.Error.WriteLine($"Model: failed ({ex.GetType().Name}). Check endpoint, credentials, model and JSON schema support.");
        return false;
    }
}

static bool Fail(string operation, HttpStatusCode code)
{
    Console.Error.WriteLine($"{operation}: HTTP {(int)code}.");
    return false;
}

static HttpClientHandler CreateHandler() => new()
{
    AllowAutoRedirect = false,
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

public sealed record ProbeFinding(string Category, string Title, double Confidence);
