using CodeReviewer.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CodeReviewer.Web.Tests;

public sealed class ConnectionCatalogTests : IDisposable
{
    private readonly string dbName = "CodeReviewerTest_" + Guid.NewGuid().ToString("N");
    private string testConnection => $"Server=localhost;Database={dbName};Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private bool created;
    private ConnectionCatalogStore CreateStore(IDataProtectionProvider provider, IConfiguration original)
    {
        if (!created)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"); connection.Open();
            using var command=connection.CreateCommand(); command.CommandText=$"CREATE DATABASE [{dbName}]"; command.ExecuteNonQuery(); created=true;
        }
        var config = new ConfigurationBuilder().AddConfiguration(original).AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"] = testConnection }).Build();
        var database=new SqlDataStore(config,provider); database.Initialize();
        return new ConnectionCatalogStore(provider,config,database);
    }
    public void Dispose()
    {
        if (!created || !System.Text.RegularExpressions.Regex.IsMatch(dbName,@"^CodeReviewerTest_[0-9a-f]{32}$")) return;
        Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();
        using var connection=new Microsoft.Data.SqlClient.SqlConnection("Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"); connection.Open();
        using var command=connection.CreateCommand(); command.CommandText=$"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}]"; command.ExecuteNonQuery();
    }

    [Fact]
    public void HistoryPagesInSqlAndLoadsReportById()
    {
        var protection=new EphemeralDataProtectionProvider();
        CreateStore(protection,new ConfigurationBuilder().Build());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"]=testConnection }).Build();
        var database=new SqlDataStore(config,protection);
        var mr=new MergeRequestItem(Guid.NewGuid(),"23",4,"test","head","https://example.test/mr/4","author");
        var ids=Enumerable.Range(0,17).Select(_=>database.StartReview(mr,"model")).ToArray();
        database.FinishReview(ids[0],"Completed",0,[]);
        var first=database.History(take:15,includeDetails:false);
        var second=database.History(skip:15,take:15,includeDetails:false);
        Assert.Equal(17,database.ReviewCount()); Assert.Equal(15,first.Count); Assert.Equal(2,second.Count);
        Assert.Empty(first.Select(x=>x.Id).Intersect(second.Select(x=>x.Id)));
        Assert.Equal(ids[0],Assert.Single(database.History(id:ids[0])).Id);
        Assert.Single(database.History(status:"Completed"));
    }

    [Fact]
    public void SqlQueueRetainsProfileSnapshotAndRecoversClaimedWork()
    {
        var protection=new EphemeralDataProtectionProvider();
        var catalog=CreateStore(protection,new ConfigurationBuilder().Build());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"]=testConnection }).Build();
        var database=new SqlDataStore(config,protection);
        var model=new AiConnection {Name="model",Endpoint="https://example.test/v1",Model="test",ApiKey="test-key"}; catalog.Upsert(model);
        var profile=new ReviewProfile(); database.SaveProfile(profile);
        var mr=new MergeRequestItem(Guid.NewGuid(),"23",4,"test","head","https://example.test/mr/4","author");
        var queue=new ReviewJobQueue(config,database,catalog); queue.Initialize();
        var id=queue.Enqueue(mr,model.Id,profile.Id,publishToMergeRequest:true);
        profile.Name="changed"; database.SaveProfile(profile);
        var first=queue.Claim()!; Assert.Equal(id,first.RunId); Assert.NotEqual("changed",first.Profile.Name); Assert.True(first.PublishToMergeRequest); Assert.Null(queue.Claim());
        queue.Initialize(); Assert.Equal(id,queue.Claim()!.RunId);
        queue.Cancel(id); Assert.True(queue.IsCancelled(id)); queue.Finish(id,false); Assert.Null(queue.Claim());
        var recovered=queue.Enqueue(mr,model.Id,profile.Id);
        var recoveredJob=queue.Claim()!;
        Assert.Equal(recovered,recoveredJob.RunId); Assert.False(recoveredJob.PublishToMergeRequest);
        var finding=new ReviewFinding("correctness","medium",0.95,"Example.cs",1,"defect","details","fix");
        database.FinishReview(recovered,"Cancelled",1,[finding]);
        queue.Finish(recovered,true);
        Assert.Equal(recovered,queue.Claim()!.RunId);
        database.FinishReview(recovered,"Completed",1,[finding]);
        Assert.Single(database.History(id:recovered).Single().Findings);
        queue.Finish(recovered,true);
        Assert.Null(queue.Claim());
    }

    [Fact]
    public void PersistsReviewFindingsAndRecoversInterruptedExecution()
    {
        var provider = new EphemeralDataProtectionProvider();
        var store = CreateStore(provider, new ConfigurationBuilder().Build());
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"] = testConnection }).Build();
        var database = new SqlDataStore(config,provider);
        var mr = new MergeRequestItem(Guid.NewGuid(),"23",4,"test request","abc","https://gitlab.example/mr/4","tester");
        var completed = database.StartReview(mr,"test-model");
        database.FinishReview(completed,"Completed",1,[new("correctness","medium",0.95,"Example.cs",10,"Defect","Details","Fix","changedCode()","runtime failure","new call")]);
        var interrupted = database.StartReview(mr,"test-model");
        var reopened = new SqlDataStore(config,provider);
        reopened.Initialize();
        reopened.RecoverInterruptedReviews();
        var history = reopened.History();
        Assert.Single(history.Single(x => x.Id == completed).Findings);
        Assert.Equal("changedCode()",history.Single(x=>x.Id==completed).Findings[0].Evidence);
        Assert.Equal("Interrupted",history.Single(x => x.Id == interrupted).Status);
        Assert.Throws<InvalidOperationException>(() => reopened.FinishReview(completed,"Completed",1,[]));
        Assert.Single(reopened.History().Single(x => x.Id == completed).Findings);
    }

    [Fact]
    public void TracksAddedIncrementOperatorsAtCorrectDiffLine()
    {
        var lines = AiReviewService.GetAddedLineNumbers("--- a.cs\n+++ b.cs\n@@ -4,1 +4,3 @@\n context\n+++counter;\n+return counter;\n");
        Assert.Equal(new[] {5,6}, lines.OrderBy(x => x));
    }

    [Fact]
    public void ProfilesPersistAndHistoricalSettingsDoNotChangeAfterEditing()
    {
        var protection=new EphemeralDataProtectionProvider();
        CreateStore(protection,new ConfigurationBuilder().Build());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"]=testConnection }).Build();
        var database=new SqlDataStore(config,protection);
        var profile=database.Profiles().Single(x=>x.Strictness=="Light");
        Assert.False(profile.Accepts(new("correctness","medium",0.99,"a.cs",1,"a","b","c")));
        Assert.True(profile.Accepts(new("correctness","high",0.95,"a.cs",1,"a","b","c")));
        profile.Prompt="Original prompt"; database.SaveProfile(profile);
        var mr=new MergeRequestItem(Guid.NewGuid(),"repo",1,"Example","head","https://example.org","test");
        var run=database.StartReview(mr,"model-a",profile:profile);
        database.FinishReview(run,"Completed",0,[]);
        profile.Prompt="Edited prompt"; profile.MaxFiles=12; database.SaveProfile(profile);
        var reopened=new SqlDataStore(config,protection); reopened.Initialize();
        Assert.Equal("Edited prompt",reopened.Profiles().Single(x=>x.Id==profile.Id).Prompt);
        Assert.Equal("Original prompt",reopened.History().Single(x=>x.Id==run).Profile!.Prompt);
        profile.MinimumConfidence=double.NaN;
        Assert.Throws<ArgumentException>(()=>database.SaveProfile(profile));
    }

    [Fact]
    public void DiffParserSeparatesOldAndNewLinesAndPreservesIndentation()
    {
        var rows=DiffDocument.Parse("@@ -10,2 +10,3 @@\n-    old();\n+    new();\n+++counter;\n context();\n");
        Assert.Equal(10,rows[1].OldLine); Assert.Null(rows[1].NewLine);
        Assert.Equal("    new();",rows[2].Text); Assert.Equal(10,rows[2].NewLine);
        Assert.Equal("++counter;",rows[3].Text); Assert.Equal(11,rows[3].NewLine);
        Assert.Equal(11,rows[4].OldLine); Assert.Equal(12,rows[4].NewLine);
    }

    [Fact]
    public void GitLabLinksUseConfiguredPublicHostAndPort()
    {
        var source=new SourceConnection { BaseUrl="https://5.160.84.190:60443" };
        Assert.Equal("https://5.160.84.190:60443/dmp_portal/dmp/-/merge_requests/4",SourceControlUrls.GitLabWebUrl(source,"https://192.168.1.155/dmp_portal/dmp/-/merge_requests/4"));
        Assert.Equal("https://5.160.84.190:60443/a?view=diff#L2",SourceControlUrls.GitLabWebUrl(source,"http://internal/a?view=diff#L2"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReviewLogsAreVisibleWhileModelIsStillRunning(bool noResponse)
    {
        var protection=new EphemeralDataProtectionProvider();
        var catalog=CreateStore(protection,new ConfigurationBuilder().Build());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"]=testConnection }).Build();
        var database=new SqlDataStore(config,protection);
        var sourceConnection=new SourceConnection { Name="test source",BaseUrl="https://source.example",Token="test-token" };
        var model=new AiConnection { Name="test model",Endpoint="https://model.example/v1",Model="test",ApiKey="test-key" };
        catalog.Upsert(sourceConnection); catalog.Upsert(model);
        var mr=new MergeRequestItem(sourceConnection.Id,"repo",4,"test","head","https://source.example/mr/4","author");
        var source=new SourceControlService(catalog,[new FakeReviewSource(mr)],database);
        var factory=new WaitingModelFactory();
        var reviewer=new AiReviewService(catalog,source,database,factory);
        var profile=new ReviewProfile { ModelTimeoutSeconds=5 }; database.SaveProfile(profile);
        Guid runId=default;
        var task=reviewer.ReviewAsync(mr,model.Id,profileId:profile.Id,onStarted:id=>{ runId=id; return Task.CompletedTask; });
        await factory.RequestReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var live=database.ReadReviewLog(runId)!;
        Assert.False(task.IsCompleted);
        Assert.Equal("Running",live.Status);
        Assert.Contains(live.Events,x=>x.File=="Test.cs" && x.TotalFiles==1);
        if(noResponse)
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10));
            var timedOut=database.ReadReviewLog(runId)!;
            Assert.Equal("Incomplete",timedOut.Status);
            Assert.Contains(timedOut.Events,e=>e.Level=="Error");
            Assert.NotNull(timedOut.FinishedUtc);
            return;
        }
        factory.Response.SetResult(new(System.Net.HttpStatusCode.OK) { Content=new StringContent("""
            {"id":"test","object":"chat.completion","created":1,"model":"test","choices":[{"index":0,"message":{"role":"assistant","content":"{\"findings\":[]}"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}
            """,System.Text.Encoding.UTF8,"application/json") });
        await task.WaitAsync(TimeSpan.FromSeconds(10));
        var finished=new SqlDataStore(config,protection).ReadReviewLog(runId)!;
        Assert.Equal("Completed",finished.Status);
        Assert.Equal(1,finished.Events.Last().CompletedFiles);
        Assert.True(finished.Events.Count>live.Events.Count);
    }

    [Theory]
    [InlineData(false,0)]
    [InlineData(true,3)]
    public async Task FullPipelinePersistsImpactWithoutDefectsAndHonorsPublicationChoice(bool publishToMergeRequest,int expectedPublications)
    {
        var protection=new EphemeralDataProtectionProvider();
        var catalog=CreateStore(protection,new ConfigurationBuilder().Build());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"]=testConnection }).Build();
        var database=new SqlDataStore(config,protection);
        var sourceConnection=new SourceConnection {Name="source",BaseUrl="https://example.test",Token="test"};
        var model=new AiConnection {Name="model",Endpoint="https://example.test/v1",Model="model",ApiKey="test"};
        catalog.Upsert(sourceConnection); catalog.Upsert(model);
        var contexts=new ProjectContextStore(database);
        contexts.Save(sourceConnection.Id,"repo",new() {Name="Product",Features=[new() {Name="Checkout",PathPrefixes=["src/Checkout/"],UiPath="/checkout",Critical=true,Scenarios=[new("Submit valid order","Order saved once")]}]});
        Assert.Empty(contexts.Read(sourceConnection.Id,"other-repo").Features);
        var mr=new MergeRequestItem(sourceConnection.Id,"repo",1,"change","head","https://example.test/mr/1","author");
        var diff="@@ -0,0 +1 @@\n+int x=1;";
        var provider=new FakeReviewSource(mr,[new("src/Checkout/Handler.cs",diff,false,false),new("docs/spec.md",diff,false,false),new("Generated.cs",diff,true,false)]);
        var source=new SourceControlService(catalog,[provider],database);
        var factory=new WaitingModelFactory();
        factory.Response.SetResult(new(System.Net.HttpStatusCode.OK) {Content=new StringContent("""
            {"id":"test","object":"chat.completion","created":1,"model":"test","choices":[{"index":0,"message":{"role":"assistant","content":"{\"findings\":[],\"manualTests\":[]}"},"finish_reason":"stop"}]}
            """,System.Text.Encoding.UTF8,"application/json")});
        Guid id=default;
        await new AiReviewService(catalog,source,database,factory).ReviewAsync(mr,model.Id,onStarted:value=>{id=value;return Task.CompletedTask;},publishToMergeRequest:publishToMergeRequest);
        var run=new SqlDataStore(config,protection).History(id:id).Single();
        Assert.Equal("Completed",run.Status); Assert.Equal(1,run.FilesReviewed); Assert.Equal(1,factory.Requests); Assert.Equal(expectedPublications,provider.Publications);
        Assert.Empty(run.Findings); Assert.Equal("High",run.Impact!.Risk); Assert.Single(run.Impact.Direct); Assert.Single(run.Impact.ManualTests);
        Assert.Contains("Checkout",ReviewCommentFormatter.Build(run)); Assert.Contains("ریسک رگرسیون: زیاد",ReviewCommentFormatter.Build(run));
        contexts.Save(sourceConnection.Id,"repo",new() {Name="Changed"});
        Assert.Equal("Product",database.ReadSnapshot<ProjectContext>(sourceConnection.Id,$"context:{id}")!.Name);
    }

    private sealed class FakeReviewSource(MergeRequestItem mr,IReadOnlyList<ChangedFile>? files=null) : ISourceControlProvider
    {
        public int Publications {get;private set;}
        public string Kind=>"GitLab";
        public Task<string> PublishReviewAsync(SourceConnection c,ReviewRun run,CancellationToken ct) { Publications++; return Task.FromResult(mr.WebUrl); }
        public Task<IReadOnlyList<RepositoryItem>> GetRepositoriesAsync(SourceConnection c,CancellationToken ct)=>Task.FromResult<IReadOnlyList<RepositoryItem>>([]);
        public Task<IReadOnlyList<MergeRequestItem>> GetOpenRequestsAsync(SourceConnection c,string repo,CancellationToken ct)=>Task.FromResult<IReadOnlyList<MergeRequestItem>>([mr]);
        public Task<IReadOnlyList<ChangedFile>> GetChangesAsync(SourceConnection c,string repo,int number,CancellationToken ct)=>Task.FromResult<IReadOnlyList<ChangedFile>>(files ?? [new("Test.cs","@@ -0,0 +1 @@\n+int x = 1;",false,false)]);
        public Task<string> TestAsync(SourceConnection c,CancellationToken ct)=>Task.FromResult("ok");
    }
    private sealed class WaitingModelFactory : IHttpClientFactory
    {
        public int Requests;
        public TaskCompletionSource<bool> RequestReceived {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<HttpResponseMessage> Response {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public HttpClient CreateClient(string name)=>new(new WaitingHandler(this));
        private sealed class WaitingHandler(WaitingModelFactory owner) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) { Interlocked.Increment(ref owner.Requests); owner.RequestReceived.TrySetResult(true); return owner.Response.Task.WaitAsync(ct); }
        }
    }

    [Fact]
    public void ManualReviewWorksWithoutModelAndKeepsVersionedNotes()
    {
        var protection = new EphemeralDataProtectionProvider();
        var catalog = CreateStore(protection, new ConfigurationBuilder().Build());
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:CodeReviewer"] = testConnection }).Build();
        var database = new SqlDataStore(config,protection);
        var source = new SourceControlService(catalog,[],database);
        using var services = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(
            Microsoft.Extensions.DependencyInjection.HttpClientFactoryServiceCollectionExtensions.AddHttpClient(new Microsoft.Extensions.DependencyInjection.ServiceCollection()));
        var factory = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IHttpClientFactory>(services);
        var workspace = new ReviewWorkspaceService(source,database,catalog,factory);
        var request = new MergeRequestItem(Guid.NewGuid(),"sample/repo",12,"Review manually","head-a","https://example.org/pr/12","writer");
        workspace.SaveNote(request,"Example.cs",5,"Needs a null check","reviewer");
        workspace.SaveDecision(request,"ChangesRequested","Please handle empty input","reviewer");
        var nextVersion=request with { Sha="head-b" };
        workspace.SaveNote(nextVersion,"",null,"Review new version","reviewer");
        var reopened = new SqlDataStore(config,protection).ReviewEntries(request);
        Assert.Equal(3,reopened.Count);
        Assert.Equal(2,reopened.Count(x=>x.Sha=="head-a"));
        Assert.Equal("ChangesRequested",reopened.Single(x=>x.Kind=="Decision").Decision);
        Assert.Empty(catalog.Snapshot().Models);
        Assert.Throws<ArgumentException>(()=>workspace.SaveDecision(request with { Sha="" },"Reviewed","summary","reviewer"));
        Assert.Throws<ArgumentException>(()=>workspace.SaveNote(request,"",null,"   ","reviewer"));
    }
    [Fact]
    public void SavesMultipleConnectionsAndPreservesSecretsDuringEdit()
    {
        var previous = Environment.GetEnvironmentVariable("CODEREVIEWER_DATA_DIR");
        var dir = Path.Combine(Path.GetTempPath(), "CodeReviewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Environment.SetEnvironmentVariable("CODEREVIEWER_DATA_DIR", dir);
        try
        {
            var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(dir, "keys")));
            var config = new ConfigurationBuilder().Build();
            var store = CreateStore(provider, config);
            var gitlab = new SourceConnection { Name = "GitLab", Provider = "GitLab", BaseUrl = "https://gitlab.example", Token = "secret-one" };
            var github = new SourceConnection { Name = "GitHub", Provider = "GitHub", BaseUrl = "https://api.github.com", Token = "secret-two" };
            store.Upsert(gitlab);
            store.Upsert(github);
            store.SetDefaultSource(github.Id);
            store.Upsert(new SourceConnection { Id = gitlab.Id, Name = "Renamed", Provider = "GitLab", BaseUrl = gitlab.BaseUrl, Token = "" });

            var ai = new AiConnection { Name = "Local", Provider = "OpenAICompatible", Endpoint = "https://model.example/v1", Model = "reviewer", ApiKey = "model-secret" };
            store.Upsert(ai);
            store.Upsert(new AiConnection { Id = ai.Id, Name = "Renamed AI", Provider = ai.Provider, Endpoint = ai.Endpoint, Model = ai.Model });

            var reopened = CreateStore(provider, config).Snapshot();
            Assert.Equal(2, reopened.Sources.Count);
            Assert.Equal(github.Id, reopened.DefaultSourceId);
            Assert.Equal("secret-one", reopened.Sources.Single(x => x.Id == gitlab.Id).Token);
            Assert.Equal("model-secret", reopened.Models.Single().ApiKey);
            using var sql = new Microsoft.Data.SqlClient.SqlConnection(testConnection); sql.Open(); using var command = sql.CreateCommand(); command.CommandText = "SELECT ProtectedSecret FROM dbo.Connections"; using var reader = command.ExecuteReader(); while(reader.Read()) Assert.DoesNotContain("secret-one", reader.GetString(0));

            store.RemoveSource(github.Id);
            Assert.Equal(gitlab.Id, store.Snapshot().DefaultSourceId);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CODEREVIEWER_DATA_DIR", previous);
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void RejectsInsecureEndpoints()
    {
        var previous = Environment.GetEnvironmentVariable("CODEREVIEWER_DATA_DIR");
        var dir = Path.Combine(Path.GetTempPath(), "CodeReviewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Environment.SetEnvironmentVariable("CODEREVIEWER_DATA_DIR", dir);
        try
        {
            var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(dir, "keys")));
            var store = CreateStore(provider, new ConfigurationBuilder().Build());
            Assert.Throws<ArgumentException>(() => store.Upsert(new SourceConnection { Name = "Bad", BaseUrl = "http://gitlab.local", Token = "x" }));
            Assert.Throws<ArgumentException>(() => store.Upsert(new AiConnection { Name = "Bad", Endpoint = "http://model.local", Model = "x", ApiKey = "x" }));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CODEREVIEWER_DATA_DIR", previous);
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void MigratesLegacySettingsAndReassignsDisabledDefault()
    {
        var previous = Environment.GetEnvironmentVariable("CODEREVIEWER_DATA_DIR");
        var dir = Path.Combine(Path.GetTempPath(), "CodeReviewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Environment.SetEnvironmentVariable("CODEREVIEWER_DATA_DIR", dir);
        try
        {
            var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(dir, "keys")));
            var legacy = "{\"GitLabUrl\":\"https://gitlab.example\",\"GitLabToken\":\"old-token\",\"ModelEndpoint\":\"https://model.example/v1\",\"ModelName\":\"reviewer\",\"ModelKey\":\"old-key\"}";
            File.WriteAllText(Path.Combine(dir, "connections.protected"),
                provider.CreateProtector("CodeReviewer.ConnectionSettings.v1").Protect(legacy));
            var store = CreateStore(provider, new ConfigurationBuilder().Build());
            var initial = store.Snapshot();
            Assert.Single(initial.Sources);
            Assert.Single(initial.Models);
            Assert.Equal("old-token", initial.Sources[0].Token);

            var other = new SourceConnection { Name = "Other", Provider = "GitHub", BaseUrl = "https://api.github.com", Token = "new-token" };
            store.Upsert(other);
            store.SetDefaultSource(other.Id);
            var disabled = new SourceConnection { Id = other.Id, Name = other.Name, Provider = other.Provider, BaseUrl = other.BaseUrl, Enabled = false };
            store.Upsert(disabled);
            Assert.Equal(initial.Sources[0].Id, store.Snapshot().DefaultSourceId);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CODEREVIEWER_DATA_DIR", previous);
            Directory.Delete(dir, true);
        }
    }
}



