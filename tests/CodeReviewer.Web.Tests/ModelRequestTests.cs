using Microsoft.Extensions.AI;
using CodeReviewer.Web.Services;
using System.ClientModel;

namespace CodeReviewer.Web.Tests;

public class ModelRequestTests
{
    [Fact]
    public async Task SdkFailureExposesStatusWithoutProviderBody()
    {
        using var client=new OpenAI.OpenAIClient(new ApiKeyCredential("test"),new OpenAI.OpenAIClientOptions {Endpoint=new Uri("https://example.invalid/v1"),Transport=new System.ClientModel.Primitives.HttpClientPipelineTransport(new RejectedFactory().CreateClient("test")),RetryPolicy=new System.ClientModel.Primitives.ClientRetryPolicy(0)}).GetChatClient("test").AsIChatClient();
        var exception=await Assert.ThrowsAsync<ClientResultException>(()=>client.GetResponseAsync([new(Microsoft.Extensions.AI.ChatRole.User,"test")]));
        Assert.Contains("401",ModelRequest.Describe(exception));
        Assert.DoesNotContain("private-provider-message",ModelRequest.Describe(exception));
    }
    private sealed class RejectedFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)=>new(new RejectedHandler());
    }
    private sealed class RejectedHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized) {
                Content=new StringContent("{\"error\":{\"message\":\"private-provider-message\",\"type\":\"invalid_api_key\"}}",System.Text.Encoding.UTF8,"application/json")
            });
    }
    [Fact]
    public async Task RetriesTemporaryFailureWithoutLoggingProviderBody()
    {
        var attempts=0;
        var logs=new List<string>();
        var result=await ModelRequest.RunAsync(_=>++attempts<3
            ? Task.FromException<string>(new HttpRequestException("secret response body",null,System.Net.HttpStatusCode.ServiceUnavailable))
            : Task.FromResult("ok"),logs.Add,CancellationToken.None,(_,_)=>Task.CompletedTask);
        Assert.Equal("ok",result);
        Assert.Equal(3,attempts);
        Assert.Equal(2,logs.Count);
        Assert.All(logs,x=>{Assert.Contains("503",x); Assert.DoesNotContain("secret",x);});
    }

    [Theory]
    [InlineData(401,1)]
    [InlineData(400,1)]
    [InlineData(429,3)]
    public async Task RetryIsBoundedAndDoesNotRepeatPermanentErrors(int status,int expected)
    {
        var attempts=0;
        await Assert.ThrowsAsync<HttpRequestException>(()=>ModelRequest.RunAsync<string>(_=>{
            attempts++; throw new HttpRequestException("private body",null,(System.Net.HttpStatusCode)status);
        },_=>{},CancellationToken.None,(_,_)=>Task.CompletedTask));
        Assert.Equal(expected,attempts);
    }

    [Fact]
    public async Task DeadlineIncludesBackoff()
    {
        var attempts=0;
        await Assert.ThrowsAsync<TimeoutException>(()=>ModelDeadline.RunAsync(token=>ModelRequest.RunAsync<string>(_=>{
            attempts++; throw new HttpRequestException("rate limit",null,System.Net.HttpStatusCode.TooManyRequests);
        },_=>{},token),TimeSpan.FromMilliseconds(100)));
        Assert.Equal(1,attempts);
    }
}


