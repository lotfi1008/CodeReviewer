using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CodeReviewer.Web.Services;

public sealed class ModelDiscoveryService(IHttpClientFactory clients)
{
    public async Task<IReadOnlyList<string>> ListAsync(AiConnection draft, AiConnection? saved = null, CancellationToken ct = default)
    {
        if (draft.Provider == "AzureOpenAI")
            throw new ArgumentException("در اتصال Azure، شناسهٔ Deployment را دستی وارد کنید؛ فهرست مدل‌ها لزوماً نام Deploymentهای شما نیست.");
        if (draft.Provider is not ("OpenAICompatible" or "Ollama")) throw new ArgumentException("نوع سرویس پشتیبانی نمی‌شود.");
        if (!Uri.TryCreate(draft.Endpoint.Trim().TrimEnd('/') + "/",UriKind.Absolute,out var endpoint) || endpoint.Scheme != "https" || endpoint.UserInfo.Length > 0 || endpoint.Query.Length > 0 || endpoint.Fragment.Length > 0)
            throw new ArgumentException("نشانی پایهٔ HTTPS سرویس را بدون query یا fragment وارد کنید؛ مثلاً https://server/v1.");
        var key = draft.ApiKey.Trim();
        if (key.Length == 0 && saved is not null && saved.Id == draft.Id && saved.Provider == draft.Provider &&
            Uri.TryCreate(saved.Endpoint.Trim().TrimEnd('/') + "/",UriKind.Absolute,out var savedEndpoint) && endpoint == savedEndpoint)
            key = saved.ApiKey;
        if (key.Length == 0 && draft.Provider != "Ollama") throw new ArgumentException("کلید API را وارد کنید. کلید قبلی فقط برای همان نشانی و نوع سرویس قابل استفاده است.");
        using var client = clients.CreateClient(ServiceHttp.ClientName);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(25));
        using var request=new HttpRequestMessage(HttpMethod.Get,new Uri(endpoint,"models"));
        if(key.Length>0) request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
        using var response=await client.SendAsync(request,timeout.Token);
        if(!response.IsSuccessStatusCode)
            throw new ArgumentException(response.StatusCode switch {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "دسترسی به فهرست مدل‌ها رد شد؛ کلید API و مجوز آن را بررسی کنید.",
                HttpStatusCode.NotFound => "مسیر فهرست مدل‌ها پیدا نشد؛ نشانی پایهٔ API (معمولاً با /v1) را بررسی کنید یا شناسه را دستی وارد کنید.",
                _ => $"دریافت فهرست مدل‌ها ناموفق بود (HTTP {(int)response.StatusCode})."
            });
        try
        {
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if(json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("data",out var data) || data.ValueKind != JsonValueKind.Array)
                throw new JsonException();
            return data.EnumerateArray().Where(x=>x.ValueKind==JsonValueKind.Object && x.TryGetProperty("id",out var id) && id.ValueKind==JsonValueKind.String)
                .Select(x=>x.GetProperty("id").GetString()!).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch(JsonException) { throw new ArgumentException("پاسخ سرویس، فهرست مدل معتبر نیست؛ می‌توانید شناسه را دستی وارد کنید."); }
    }
}
