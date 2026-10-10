using System.Text.Json;
using CodeReviewer.Web.Services;

namespace CodeReviewer.Web.Tests;

public sealed class ModelWireTests
{
    [Fact]
    public async Task SdkSendsCompletionBudgetToCompatibleEndpoint()
    {
        var factory = new CaptureFactory();
        await ModelDiagnostics.ProbeReviewAsync(factory, new AiConnection
        {
            Endpoint = "https://example.invalid/v1", Model = "test", ApiKey = "test-key"
        });
        using var json = JsonDocument.Parse(factory.Body!);
        Assert.Equal(2048, json.RootElement.GetProperty("max_completion_tokens").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("max_tokens", out _));
    }

    private sealed class CaptureFactory : IHttpClientFactory
    {
        public string? Body { get; set; }
        public HttpClient CreateClient(string name) => new(new CaptureHandler(this));
    }

    private sealed class CaptureHandler(CaptureFactory factory) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            factory.Body = await request.Content!.ReadAsStringAsync(ct);
            return new(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {"id":"test","object":"chat.completion","created":1,"model":"test","choices":[{"index":0,"message":{"role":"assistant","content":"{\"findings\":[]}"},"finish_reason":"stop"}]}
                """, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
