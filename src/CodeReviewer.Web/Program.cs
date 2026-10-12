using CodeReviewer.Web.Components;
using CodeReviewer.Web.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
ServiceHttp.AddClients(builder.Services, builder.Configuration);
var dataDir = DataDirectory.Resolve(builder.Configuration);
Directory.CreateDirectory(dataDir);
var protection = builder.Services.AddDataProtection()
    .SetApplicationName("CodeReviewer")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorization(options =>
{
    if (!builder.Environment.IsDevelopment())
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddSingleton<SqlDataStore>();
builder.Services.AddSingleton<ProjectContextStore>();
builder.Services.AddSingleton<ConnectionCatalogStore>();
builder.Services.AddTransient<ISourceControlProvider, GitLabProvider>();
builder.Services.AddTransient<ISourceControlProvider, GitHubProvider>();
builder.Services.AddScoped<SourceControlService>();
builder.Services.AddScoped<AiReviewService>();
builder.Services.AddScoped<ReviewPublicationService>();
builder.Services.AddSingleton<ReviewJobQueue>();
builder.Services.AddHostedService<ReviewWorker>();
builder.Services.AddScoped<ModelDiscoveryService>();
builder.Services.AddScoped<ReviewWorkspaceService>();

var app = builder.Build();
var database = app.Services.GetRequiredService<SqlDataStore>();
database.Initialize();
if(Guid.TryParse(builder.Configuration["InspectReviewTarget"],out var inspectId))
{
    var run=database.History(id:inspectId).Single();
    var connection=app.Services.GetRequiredService<ConnectionCatalogStore>().Snapshot().Sources.Single(s=>s.Id==run.MergeRequest.ConnectionId);
    if(connection.Provider!="GitLab") throw new NotSupportedException("Inspection requires GitLab.");
    using var client=app.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ServiceHttp.SourceControlClientName);
    client.BaseAddress=new Uri(connection.BaseUrl.TrimEnd('/')+"/");
    client.DefaultRequestHeaders.Add("PRIVATE-TOKEN",connection.Token);
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
    using var json=await GitLabProvider.Get(client,$"api/v4/projects/{Uri.EscapeDataString(run.MergeRequest.RepositoryId)}/merge_requests/{run.MergeRequest.Number}",timeout.Token);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {
        Number=run.MergeRequest.Number, State=json.RootElement.GetProperty("state").GetString(),
        CurrentSha=json.RootElement.GetProperty("sha").GetString(), ReviewedSha=run.MergeRequest.Sha
    }));
    return;
}
if(Guid.TryParse(builder.Configuration["PublishReview"],out var publishId))
{
    using var scope=app.Services.CreateScope();
    try { Console.WriteLine("Published: " + await scope.ServiceProvider.GetRequiredService<ReviewPublicationService>().PublishAsync(publishId)); }
    catch(Exception ex) { Console.WriteLine("Publication failed: " + ex.Message); Environment.ExitCode=1; }
    return;
}
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DiagnoseModels"))
{
    var factory=app.Services.GetRequiredService<IHttpClientFactory>();
    foreach(var model in app.Services.GetRequiredService<ConnectionCatalogStore>().Snapshot().Models.Where(x=>x.Enabled))
    {
        Console.WriteLine($"Model diagnostic: {model.Name} / {model.Model}");
        if(builder.Configuration.GetValue<bool>("DiagnosticReview")) { Console.WriteLine(await ModelDiagnostics.ProbeReviewAsync(factory,model)); continue; }
        var modes=builder.Configuration.GetValue<bool>("DiagnosticExtended") ? new[]{"plain-long","schema-long"} : new[]{"models","plain","schema","stream"};
        var results=await Task.WhenAll(modes.Select(mode=>ModelDiagnostics.ProbeAsync(factory,model,mode)));
        foreach(var result in results) Console.WriteLine(result);
    }
    return;
}
if (builder.Configuration.GetValue<bool>("CheckSourceConnections"))
{
    using var scope = app.Services.CreateScope();
    var sources = scope.ServiceProvider.GetRequiredService<SourceControlService>();
    foreach (var connection in sources.Connections)
    {
        try
        {
            var result = await sources.TestAsync(connection.Id);
            database.RecordCheck(connection.Id, true, result);
            Console.WriteLine($"{connection.Name}: {result}");
        }
        catch (Exception ex)
        {
            var result = SourceControlErrors.Describe(ex);
            database.RecordCheck(connection.Id, false, result);
            Console.WriteLine($"{connection.Name}: {result}");
            Environment.ExitCode = 1;
        }
    }
    return;
}
database.RecoverInterruptedReviews();
database.UpgradeOperationalProfiles();
app.Services.GetRequiredService<ReviewJobQueue>().Initialize();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();




