using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace CodeReviewer.Web.Services;

public sealed class SourceConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "GitLab";
    public string BaseUrl { get; set; } = "";
    public string Token { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public sealed class AiConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Provider { get; set; } = "OpenAICompatible";
    public string Endpoint { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public bool SupportsJsonSchema { get; set; } = true;
    public bool Enabled { get; set; } = true;
}

public sealed class ConnectionCatalog
{
    public List<SourceConnection> Sources { get; set; } = [];
    public List<AiConnection> Models { get; set; } = [];
    public Guid? DefaultSourceId { get; set; }
    public Guid? DefaultModelId { get; set; }
}

public sealed class ConnectionCatalogStore
{
    private readonly object gate = new();
    private readonly IDataProtector protector;
    private readonly string path;
    private readonly SqlDataStore database;
    private ConnectionCatalog catalog;

    public ConnectionCatalogStore(IDataProtectionProvider protection, IConfiguration config, SqlDataStore database)
    {
        this.database = database;
        protector = protection.CreateProtector("CodeReviewer.ConnectionCatalog.v2");
        var dir = Environment.GetEnvironmentVariable("CODEREVIEWER_DATA_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeReviewer");
        Directory.CreateDirectory(dir);
        path = Path.Combine(dir, "catalog.protected");
        var saved = database.ReadCatalog();
        if (database.CatalogInitialized()) { catalog = saved; return; }
        if (File.Exists(path))
            catalog = JsonSerializer.Deserialize<ConnectionCatalog>(protector.Unprotect(File.ReadAllText(path))) ?? new();
        else
            catalog = MigrateLegacy(protection, config, dir);
        database.SaveCatalog(catalog);
    }

    public ConnectionCatalog Snapshot()
    {
        lock (gate) return Clone(database.ReadCatalog());
    }

    public void Upsert(SourceConnection input)
    {
        Validate(input.Name, input.BaseUrl, input.Provider is "GitLab" or "GitHub");
        lock (gate)
        {
            var next = database.ReadCatalog();
            var old = next.Sources.FindIndex(x => x.Id == input.Id);
            var value = Clone(input);
            if (old >= 0 && string.IsNullOrWhiteSpace(value.Token)) value.Token = next.Sources[old].Token;
            if (value.Enabled && string.IsNullOrWhiteSpace(value.Token)) throw new ArgumentException("توکن اتصال سورس کنترل الزامی است.");
            if (old < 0) next.Sources.Add(value); else next.Sources[old] = value;
            if (!next.Sources.Any(x => x.Id == next.DefaultSourceId && x.Enabled))
                next.DefaultSourceId = next.Sources.FirstOrDefault(x => x.Enabled)?.Id;
            Persist(next);
        }
    }

    public void Upsert(AiConnection input)
    {
        Validate(input.Name, input.Endpoint, input.Provider is "OpenAICompatible" or "AzureOpenAI" or "Ollama");
        if (string.IsNullOrWhiteSpace(input.Model)) throw new ArgumentException("نام مدل الزامی است.");
        lock (gate)
        {
            var next = database.ReadCatalog();
            var old = next.Models.FindIndex(x => x.Id == input.Id);
            var value = Clone(input);
            if (old >= 0 && string.IsNullOrWhiteSpace(value.ApiKey)) value.ApiKey = next.Models[old].ApiKey;
            if (value.Enabled && value.Provider != "Ollama" && string.IsNullOrWhiteSpace(value.ApiKey))
                throw new ArgumentException("کلید API برای این سرویس الزامی است.");
            if (old < 0) next.Models.Add(value); else next.Models[old] = value;
            if (!next.Models.Any(x => x.Id == next.DefaultModelId && x.Enabled))
                next.DefaultModelId = next.Models.FirstOrDefault(x => x.Enabled)?.Id;
            Persist(next);
        }
    }

    public void SetDefaultSource(Guid id)
    {
        lock (gate)
        {
            var next = database.ReadCatalog();
            if (!next.Sources.Any(x => x.Id == id && x.Enabled)) throw new ArgumentException("اتصال فعال یافت نشد.");
            next.DefaultSourceId = id;
            Persist(next);
        }
    }

    public void SetDefaultModel(Guid id)
    {
        lock (gate)
        {
            var next = database.ReadCatalog();
            if (!next.Models.Any(x => x.Id == id && x.Enabled)) throw new ArgumentException("مدل فعال یافت نشد.");
            next.DefaultModelId = id;
            Persist(next);
        }
    }

    public void RemoveSource(Guid id)
    {
        lock (gate)
        {
            var next = database.ReadCatalog();
            next.Sources.RemoveAll(x => x.Id == id);
            if (next.DefaultSourceId == id) next.DefaultSourceId = next.Sources.FirstOrDefault(x => x.Enabled)?.Id;
            Persist(next);
        }
    }

    public void RemoveModel(Guid id)
    {
        lock (gate)
        {
            var next = database.ReadCatalog();
            next.Models.RemoveAll(x => x.Id == id);
            if (next.DefaultModelId == id) next.DefaultModelId = next.Models.FirstOrDefault(x => x.Enabled)?.Id;
            Persist(next);
        }
    }

    private void Persist(ConnectionCatalog next)
    {
        database.SaveCatalog(next);
        catalog = next;
    }

    private static void Validate(string name, string url, bool knownProvider)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام اتصال الزامی است.");
        if (!knownProvider) throw new ArgumentException("نوع سرویس پشتیبانی نمی‌شود.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("نشانی سرویس باید HTTPS باشد.");
    }

    private static ConnectionCatalog MigrateLegacy(IDataProtectionProvider protection, IConfiguration config, string dir)
    {
        var migrated = new ConnectionCatalog();
        var legacyPath = Path.Combine(dir, "connections.protected");
        string? legacyJson = null;
        if (File.Exists(legacyPath))
            legacyJson = protection.CreateProtector("CodeReviewer.ConnectionSettings.v1").Unprotect(File.ReadAllText(legacyPath));
        using var doc = JsonDocument.Parse(legacyJson ?? "{}");
        string Read(string field, string key) => doc.RootElement.TryGetProperty(field, out var p) ? p.GetString() ?? "" : config[key] ?? "";
        var url = Read("GitLabUrl", "GitLab:BaseUrl");
        var token = Read("GitLabToken", "GitLab:Token");
        if (!string.IsNullOrWhiteSpace(url))
        {
            var source = new SourceConnection { Name = "GitLab اصلی", BaseUrl = url, Token = token, Enabled = !string.IsNullOrWhiteSpace(token) };
            migrated.Sources.Add(source);
            migrated.DefaultSourceId = source.Enabled ? source.Id : null;
        }
        var endpoint = Read("ModelEndpoint", "AI:Endpoint");
        var model = Read("ModelName", "AI:Model");
        var key = Read("ModelKey", "AI:ApiKey");
        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(model))
        {
            var ai = new AiConnection { Name = "مدل اصلی", Endpoint = endpoint, Model = model, ApiKey = key };
            migrated.Models.Add(ai);
            migrated.DefaultModelId = ai.Id;
        }
        return migrated;
    }

    private static ConnectionCatalog Clone(ConnectionCatalog value) => JsonSerializer.Deserialize<ConnectionCatalog>(JsonSerializer.Serialize(value))!;
    private static SourceConnection Clone(SourceConnection value) => JsonSerializer.Deserialize<SourceConnection>(JsonSerializer.Serialize(value))!;
    private static AiConnection Clone(AiConnection value) => JsonSerializer.Deserialize<AiConnection>(JsonSerializer.Serialize(value))!;
}
