using System.Net;
using CodeReviewer.Web.Services;

namespace CodeReviewer.Web.Tests;

public sealed class ModelDiscoveryTests
{
    private sealed class Factory(Func<HttpRequestMessage,HttpResponseMessage> respond) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(ServiceHttp.ClientName,name);
            return new HttpClient(new Handler(respond));
        }
    }
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) => Task.FromResult(respond(request));
    }
    private static AiConnection Draft() => new() { Endpoint="https://internal.example/proxy/v1",ApiKey="test-key" };

    [Fact]
    public async Task UsesDraftEndpointAndKeyWithoutRequiringAModel()
    {
        var service=new ModelDiscoveryService(new Factory(request=> {
            Assert.Equal("https://internal.example/proxy/v1/models",request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer",request.Headers.Authorization!.Scheme);
            Assert.Equal("test-key",request.Headers.Authorization.Parameter);
            return new(HttpStatusCode.OK) { Content=new StringContent("{\"data\":[{\"id\":\"z-model\"},{\"id\":\"a-model\"},{\"id\":\"z-model\"},{\"object\":\"ignored\"}]}") };
        }));
        Assert.Equal(new[]{"a-model","z-model"},await service.ListAsync(Draft()));
    }

    [Fact]
    public async Task StoredKeyIsUsedOnlyForUnchangedConnection()
    {
        var saved=Draft(); var draft=Draft(); draft.Id=saved.Id; draft.ApiKey="";
        var calls=0;
        var service=new ModelDiscoveryService(new Factory(request=> { calls++; Assert.Equal("test-key",request.Headers.Authorization!.Parameter); return new(HttpStatusCode.OK){ Content=new StringContent("{\"data\":[]}") }; }));
        await service.ListAsync(draft,saved);
        draft.Endpoint="https://different.example/v1";
        await Assert.ThrowsAsync<ArgumentException>(()=>service.ListAsync(draft,saved));
        Assert.Equal(1,calls);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{\"data\":{}}")]
    public async Task InvalidResponsesHaveActionableErrors(string body)
    {
        var service=new ModelDiscoveryService(new Factory(_=>new(HttpStatusCode.OK){Content=new StringContent(body)}));
        await Assert.ThrowsAsync<ArgumentException>(()=>service.ListAsync(Draft()));
    }

    [Fact]
    public async Task DoesNotExposeProviderErrorBody()
    {
        var service=new ModelDiscoveryService(new Factory(_=>new(HttpStatusCode.Unauthorized){Content=new StringContent("secret provider details")}));
        var ex=await Assert.ThrowsAsync<ArgumentException>(()=>service.ListAsync(Draft()));
        Assert.DoesNotContain("secret",ex.Message);
    }
}
