using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace CodeReviewer.Web.Services;

public static class ModelDiagnostics
{
    public static async Task<string> ProbeReviewAsync(IHttpClientFactory clients,AiConnection model)
    {
        using var chat=AiReviewService.CreateClient(model,clients);
        var clock=Stopwatch.StartNew();
        try
        {
            var response=await ModelDeadline.RunAsync(token=>chat.GetResponseAsync<ReviewOutput>(
                "Review this synthetic C# diff. Return findings only for definite defects. File must be Sample.cs, line 1. Categories: correctness. Severity: medium, high. Confidence 0 to 1. Use concise descriptions. Return empty findings if unsure.\n@@ -0,0 +1 @@\n+public int Divide(int a, int b) => a / b;",
                new ChatOptions { MaxOutputTokens=2048 },useJsonSchemaResponseFormat:model.SupportsJsonSchema,cancellationToken:token),TimeSpan.FromSeconds(60));
            return $"SDK review: complete={clock.Elapsed.TotalSeconds:F1}s; findings={response.Result?.Findings?.Count}; finish={response.FinishReason}; output_tokens={response.Usage?.OutputTokenCount}";
        }
        catch(Exception ex) { return $"SDK review: {ex.GetType().Name}; elapsed={clock.Elapsed.TotalSeconds:F1}s; inner={ex.InnerException?.GetType().Name}"; }
    }
    // Synthetic prompts only; never send repository data during connection diagnosis.
    public static async Task<string> ProbeAsync(IHttpClientFactory clients,AiConnection model,string mode)
    {
        using var client=clients.CreateClient(ServiceHttp.ClientName);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(mode.EndsWith("-long") ? 40 : 20));
        var clock=Stopwatch.StartNew(); var stage="connection/headers";
        using var request=new HttpRequestMessage(mode=="models"?HttpMethod.Get:HttpMethod.Post,new Uri(new Uri(model.Endpoint.TrimEnd('/')+"/"),mode=="models"?"models":"chat/completions"));
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",model.ApiKey);
        if(mode!="models")
        {
            var payload=new Dictionary<string,object> { ["model"]=model.Model,["messages"]=new[]{new {role="user",content=mode.StartsWith("schema")?"Return a JSON object with ok=true.":"Reply with OK only."}},["max_tokens"]=mode.EndsWith("-long") ? 512 : 64,["stream"]=mode=="stream" };
            if(mode.StartsWith("schema")) payload["response_format"]=new {type="json_schema",json_schema=new {name="connection_probe",strict=true,schema=new {type="object",properties=new {ok=new {type="boolean"}},required=new[]{"ok"},additionalProperties=false}}};
            request.Content=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json");
        }
        try
        {
            using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
            var headers=$"HTTP {(int)response.StatusCode}; headers={clock.Elapsed.TotalSeconds:F1}s; type={response.Content.Headers.ContentType?.MediaType}";
            stage=$"body after {headers}";
            if(mode=="stream" && response.IsSuccessStatusCode)
            {
                using var stream=await response.Content.ReadAsStreamAsync(deadline.Token);
                using var reader=new StreamReader(stream);
                while(await reader.ReadLineAsync(deadline.Token) is { } line)
                    if(line.StartsWith("data:") && line.Length>6) return $"{mode}: {headers}; first SSE event={clock.Elapsed.TotalSeconds:F1}s";
                return $"{mode}: {headers}; stream ended without data";
            }
            var body=await response.Content.ReadAsStringAsync(deadline.Token);
            using var json=JsonDocument.Parse(body);
            if(!response.IsSuccessStatusCode)
            {
                var message=json.RootElement.TryGetProperty("error",out var error) ? error.ToString() : "No structured error";
                if(!string.IsNullOrEmpty(model.ApiKey)) message=message.Replace(model.ApiKey,"[redacted]",StringComparison.Ordinal);
                return $"{mode}: {headers}; error={message[..Math.Min(message.Length,600)]}";
            }
            if(mode=="models")
            {
                var ids=json.RootElement.GetProperty("data").EnumerateArray().Select(x=>x.GetProperty("id").GetString()).ToArray();
                return $"{mode}: {headers}; count={ids.Length}; configured_model_listed={ids.Contains(model.Model)}";
            }
            var choice=json.RootElement.GetProperty("choices")[0];
            var msg=choice.GetProperty("message");
            var reasoning=msg.TryGetProperty("reasoning_content",out var rc) ? rc.ToString().Length : msg.TryGetProperty("reasoning",out var rr) ? rr.ToString().Length : 0;
            var usage=json.RootElement.TryGetProperty("usage",out var u) ? u.ToString() : "none";
            return $"{mode}: {headers}; complete={clock.Elapsed.TotalSeconds:F1}s; finish_reason={choice.GetProperty("finish_reason")}; content_length={choice.GetProperty("message").GetProperty("content").ToString().Length}; reasoning_chars={reasoning}; usage={usage}";
        }
        catch(OperationCanceledException) { return $"{mode}: TIMEOUT after {clock.Elapsed.TotalSeconds:F1}s at {stage}"; }
        catch(Exception ex) { return $"{mode}: {ex.GetType().Name} at {stage}"; }
    }
}
