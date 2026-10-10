using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CodeReviewer.Web.Services;

public sealed partial class GitLabProvider
{
    public async Task<string> PublishReviewAsync(SourceConnection connection, ReviewRun run, CancellationToken ct)
    {
        if(run.Status is not ("Running" or "Completed" or "Incomplete" or "Failed" or "TimedOut" or "Cancelled")) throw new InvalidOperationException("وضعیت بازبینی قابل انتشار نیست.");
        run=run with {Findings=ReviewCommentFormatter.Important(run)};
        using var client = Client(connection);
        var mr = run.MergeRequest;
        var root = $"api/v4/projects/{Uri.EscapeDataString(mr.RepositoryId)}/merge_requests/{mr.Number}";
        async Task<JsonDocument> CheckHead()
        {
            var current = await Get(client, root, ct);
            if(string.IsNullOrWhiteSpace(mr.Sha) || current.RootElement.GetProperty("sha").GetString()!=mr.Sha || current.RootElement.GetProperty("state").GetString()!="opened")
            { current.Dispose(); throw new InvalidOperationException("نسخه یا وضعیت مرج ریکوست تغییر کرده است؛ نتیجهٔ قدیمی منتشر نمی‌شود. دوباره بازبینی کنید."); }
            return current;
        }
        using var initial = await CheckHead();
        using var user = await Get(client, "api/v4/user", ct);
        var author = user.RootElement.GetProperty("id").GetInt64();
        var notes = await ListAll(client, root+"/notes", ct);
        var discussions = run.Findings.Count>0 ? await ListAll(client, root+"/discussions", ct) : [];
        var ownNotes = notes.Where(n=>n.GetProperty("author").GetProperty("id").GetInt64()==author).ToList();
        var ownBodies = discussions.SelectMany(d=>d.GetProperty("notes").EnumerateArray()).Where(n=>n.GetProperty("author").GetProperty("id").GetInt64()==author).Select(n=>n.GetProperty("body").GetString() ?? "").ToList();
        var diffs = run.Findings.Count>0 ? await ListAll(client, root+"/diffs", ct) : [];
        foreach(var finding in run.Findings)
        {
            var marker = "<!-- code-reviewer:finding:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {mr.Sha,finding.File,finding.Line,finding.Category,Title=finding.Title.Trim().ToLowerInvariant()})))) + " -->";
            if(ownBodies.Any(b=>b.Contains(marker,StringComparison.Ordinal))) continue;
            var diff = diffs.SingleOrDefault(d=>d.GetProperty("new_path").GetString()==finding.File);
            if(diff.ValueKind==JsonValueKind.Undefined || !AiReviewService.GetAddedLineNumbers(diff.GetProperty("diff").GetString() ?? "").Contains(finding.Line))
                throw new InvalidOperationException("خط یافته در Diff فعلی معتبر نیست؛ انتشار متوقف شد.");
            using var current = await CheckHead();
            var refs = current.RootElement.GetProperty("diff_refs");
            if(refs.GetProperty("head_sha").GetString()!=mr.Sha) throw new InvalidOperationException("نسخهٔ Diff هنوز آماده نیست؛ انتشار را دوباره امتحان کنید.");
            var body = marker + "\n### بازبینی هوشمند کد\n" + FindingText(finding);
            using var created = await Write(client, HttpMethod.Post, root+"/discussions", new { body, position=new {
                position_type="text", base_sha=refs.GetProperty("base_sha").GetString(), start_sha=refs.GetProperty("start_sha").GetString(), head_sha=mr.Sha,
                old_path=diff.GetProperty("old_path").GetString(), new_path=finding.File, new_line=finding.Line
            }}, ct);
            ownBodies.Add(body);
        }
        var summaryMarker = $"<!-- code-reviewer:summary:{run.Profile?.Id.ToString() ?? "default"} -->";
        var summary = summaryMarker + "\n" + ReviewCommentFormatter.Build(run);
        var existing = ownNotes.FirstOrDefault(n=>(n.GetProperty("body").GetString() ?? "").StartsWith(summaryMarker,StringComparison.Ordinal));
        using var beforeSummary = await CheckHead();
        long id;
        if(existing.ValueKind != JsonValueKind.Undefined && existing.GetProperty("body").GetString()==summary) id=existing.GetProperty("id").GetInt64();
        else
        {
            var path=existing.ValueKind==JsonValueKind.Undefined ? root+"/notes" : root+"/notes/"+existing.GetProperty("id").GetInt64();
            using var result=await Write(client,existing.ValueKind==JsonValueKind.Undefined ? HttpMethod.Post : HttpMethod.Put,path,new {body=summary},ct);
            id=result.RootElement.GetProperty("id").GetInt64();
        }
        var url=SourceControlUrls.GitLabWebUrl(connection,initial.RootElement.GetProperty("web_url").GetString() ?? mr.WebUrl)+"#note_"+id;
        await PublishDecision(client,root,run,url,CheckHead,ct);
        return url;
    }

    // Encode model-controlled text and quote every line, preventing quick actions and mentions.
    private static string Quote(string text) => string.Join("\n",WebUtility.HtmlEncode(text).Replace("@","&#64;").Replace("\r","").Split('\n').Select(l=>"> "+l));
    private static string FindingText(ReviewFinding f) => Quote($"{f.Title}\nشدت: {f.Severity} | دسته: {f.Category} | اطمینان: {f.Confidence:P0}\n{f.File}:{f.Line}\n{f.Description}\nپیشنهاد: {f.Suggestion}");
    internal static async Task<List<JsonElement>> ListAll(HttpClient client,string path,CancellationToken ct)
    {
        var items = new List<JsonElement>();
        string? previous=null;
        for(var page=1;true;page++)
        {
            using var json=await Get(client,path+(path.Contains('?') ? "&" : "?")+$"per_page=100&page={page}",ct);
            var signature=json.RootElement.GetRawText();
            if(signature==previous && json.RootElement.GetArrayLength()>0) throw new InvalidOperationException("سرویس صفحهٔ تکراری برگرداند؛ انتشار برای جلوگیری از تکرار متوقف شد.");
            previous=signature;
            var batch=json.RootElement.EnumerateArray().Select(x=>x.Clone()).ToArray(); items.AddRange(batch);
            if(batch.Length<100) return items;
        }
    }
    private static async Task<JsonDocument> Write(HttpClient client,HttpMethod method,string path,object body,CancellationToken ct)
    {
        using var request=new HttpRequestMessage(method,path) { Content=JsonContent.Create(body) };
        using var response=await client.SendAsync(request,ct);
        if(!response.IsSuccessStatusCode) throw new InvalidOperationException($"انتشار کامنت با HTTP {(int)response.StatusCode} ناموفق بود؛ دسترسی نوشتن توکن و وضعیت MR را بررسی کنید.");
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct),cancellationToken:ct);
    }
}




