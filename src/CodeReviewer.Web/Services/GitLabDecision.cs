using System.Text.Json;

namespace CodeReviewer.Web.Services;

public sealed partial class GitLabProvider
{
    private static async Task PublishDecision(HttpClient client,string root,ReviewRun run,string url,
        Func<Task<JsonDocument>> checkHead,CancellationToken ct)
    {
        using var current=await checkHead();
        var labels=current.RootElement.TryGetProperty("labels",out var values)
            ? values.EnumerateArray().Select(x=>x.GetString() ?? "").ToArray() : [];
        var desired=ReviewDecision.Label(run);
        var old=labels.Where(l=>l.StartsWith(ReviewDecision.LabelPrefix(run),StringComparison.Ordinal) && l!=desired).ToArray();
        if(!labels.Contains(desired) || old.Length>0)
        {
            using var updated=await Write(client,HttpMethod.Put,root,new {add_labels=desired,remove_labels=string.Join(",",old)},ct);
        }
        var project=$"api/v4/projects/{Uri.EscapeDataString(run.MergeRequest.RepositoryId)}";
        var name=ReviewDecision.Context(run);
        var state=ReviewDecision.CommitState(run);
        var description=ReviewDecision.Title(run);
        var statuses=await ListAll(client,$"{project}/repository/commits/{run.MergeRequest.Sha}/statuses?name={Uri.EscapeDataString(name)}",ct);
        // Same context is updated idempotently; other checks and approvals are untouched.
        if(statuses.Any(s=>s.GetProperty("name").GetString()==name && s.GetProperty("status").GetString()==state
            && s.TryGetProperty("target_url",out var target) && target.GetString()==url
            && s.TryGetProperty("description",out var desc) && desc.GetString()==description)) return;
        using var beforeStatus=await checkHead();
        using var status=await Write(client,HttpMethod.Post,$"{project}/statuses/{run.MergeRequest.Sha}",
            new {state,name,description,target_url=url},ct);
    }
}
