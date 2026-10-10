using System.Net;
using System.Text.Json;
using CodeReviewer.Web.Services;

namespace CodeReviewer.Web.Tests;

public sealed class PublicationTests
{
    private static ReviewRun Run(bool finding=true) => new(Guid.NewGuid(),new(Guid.NewGuid(),"23",4,"MR","head","https://public.example/mr/4","author"),"model","ConnectedModel","Completed",DateTime.UtcNow,DateTime.UtcNow,1,"یک فایل بررسی شد",finding ? [new("correctness","high",.9,"New.cs",1,"issue","/close\n@user","fix","code","concrete impact","changed behavior")] : [],new ReviewProfile(),[new("New.cs","بررسی شد",1,0,"تغییر اعتبارسنجی","بررسی شرط ورودی در خط ۱")],new([],[],[],[],"Medium",false));
    private sealed class Server : HttpMessageHandler,IHttpClientFactory
    {
        public string Sha="head";
        public int Writes;
        public int HeadReads;
        public bool ChangeBeforeWrite;
        public bool RejectWrite;
        public List<object> Notes=[];
        public List<object> Discussions=[];
        public List<string> Labels=[];
        public List<object> Statuses=[];
        public int StatusWrites;
        public HttpClient CreateClient(string name)=>new(this,false);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var path=request.RequestUri!.AbsolutePath;
            object payload;
            if(request.Method!=HttpMethod.Get)
            {
                Writes++;
                if(RejectWrite) return new(HttpStatusCode.Forbidden);
                using var body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                if(path.Contains("/statuses/")) {
                    StatusWrites++; Statuses.Clear(); Statuses.Add(new {name=body.RootElement.GetProperty("name").GetString(),status=body.RootElement.GetProperty("state").GetString(),description=body.RootElement.GetProperty("description").GetString(),target_url=body.RootElement.GetProperty("target_url").GetString()});
                    return new(HttpStatusCode.OK) {Content=new StringContent("{}")};
                }
                if(body.RootElement.TryGetProperty("add_labels",out var added)) {
                    var removed=body.RootElement.GetProperty("remove_labels").GetString()!.Split(',');
                    Labels.RemoveAll(x=>removed.Contains(x)); Labels.Add(added.GetString()!);
                    return new(HttpStatusCode.OK) {Content=new StringContent("{}")};
                }
                var text=body.RootElement.GetProperty("body").GetString()!;
                var note=new {id=101,body=text,author=new {id=1}};
                if(path.EndsWith("/discussions"))
                {
                    var position=body.RootElement.GetProperty("position");
                    Assert.Equal("Old.cs",position.GetProperty("old_path").GetString());
                    Assert.Equal(1,position.GetProperty("new_line").GetInt32());
                    Assert.False(position.TryGetProperty("old_line",out _));
                    Assert.Contains("> /close",text); Assert.DoesNotContain("@user",text);
                    payload=new {id="discussion",notes=new[]{note}}; Discussions.Add(payload);
                }
                else { Notes.Clear(); Notes.Add(note); payload=note; }
            }
            else if(path.EndsWith("/user")) payload=new {id=1};
            else if(path.EndsWith("/notes")) payload=Notes;
            else if(path.EndsWith("/discussions")) payload=Discussions;
            else if(path.EndsWith("/diffs")) payload=new[]{new {old_path="Old.cs",new_path="New.cs",diff="@@ -0,0 +1 @@\n+code"}};
            else if(path.EndsWith("/statuses")) payload=Statuses;
            else { HeadReads++; payload=new {sha=ChangeBeforeWrite && HeadReads>1 ? "new" : Sha,state="opened",labels=Labels,web_url="https://internal.example/mr/4",diff_refs=new {base_sha="base",start_sha="start",head_sha="head"}}; }
            return new(HttpStatusCode.OK) {Content=new StringContent(JsonSerializer.Serialize(payload),System.Text.Encoding.UTF8,"application/json")};
        }
    }
    private static SourceConnection Connection()=>new() {BaseUrl="https://public.example",Token="test"};
    [Fact]
    public async Task RetryDoesNotDuplicateSummaryOrInlineAndSupportsRenames()
    {
        using var server=new Server(); var provider=new GitLabProvider(server); var run=Run();
        var url=await provider.PublishReviewAsync(Connection(),run,default);
        await provider.PublishReviewAsync(Connection(),run,default);
        Assert.Equal(4,server.Writes); Assert.Single(server.Notes); Assert.Single(server.Discussions); Assert.Equal(1,server.StatusWrites);
        Assert.Equal("https://public.example/mr/4#note_101",url);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangedHeadNeverPublishes(bool changesLater)
    {
        using var server=new Server {Sha=changesLater ? "head" : "new",ChangeBeforeWrite=changesLater};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new GitLabProvider(server).PublishReviewAsync(Connection(),Run(),default));
        Assert.Equal(0,server.Writes);
    }
    [Fact]
    public async Task PermissionFailureIsExplicit()
    {
        using var server=new Server {RejectWrite=true};
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>new GitLabProvider(server).PublishReviewAsync(Connection(),Run(false),default));
        Assert.Contains("403",error.Message);
    }
    [Fact]
    public async Task RunningThenIncompletePublishesInlineAndNeverPassesCheck()
    {
        using var server=new Server(); server.Labels.Add("team-label");
        var provider=new GitLabProvider(server); var run=Run() with {Status="Running",FinishedUtc=null};
        await provider.PublishReviewAsync(Connection(),run,default);
        Assert.Single(server.Discussions);
        Assert.Contains("failed",JsonSerializer.Serialize(server.Statuses));
        run=run with {Status="Incomplete"};
        await provider.PublishReviewAsync(Connection(),run,default);
        Assert.Single(server.Discussions); Assert.Single(server.Notes);
        Assert.Contains("team-label",server.Labels);
        Assert.Contains("ناقص",JsonSerializer.Serialize(server.Notes,new JsonSerializerOptions {Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
        Assert.Equal("failed",ReviewDecision.CommitState(run));
    }
    [Fact]
    public void IncompleteWithoutFindingsCannotPassAndSuggestionsRespectThreshold()
    {
        Assert.Equal("failed",ReviewDecision.CommitState(Run(false) with {Status="Incomplete"}));
        Assert.Equal("running",ReviewDecision.CommitState(Run(false) with {Status="Running"}));
        var suggestion=Run(); suggestion.Profile!.BlockingSeverity="critical";
        Assert.Equal("success",ReviewDecision.CommitState(suggestion));
        Assert.Equal("failed",ReviewDecision.CommitState(Run()));
    }
}


