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
        Assert.Equal(4,logs.Count);
        Assert.Equal(2,logs.Count(x=>x.Contains("503")));
        Assert.All(logs,x=>Assert.DoesNotContain("secret",x));
    }

    [Theory]
    [InlineData(401,1)]
    [InlineData(400,1)]
    [InlineData(429,4)]
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

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public async Task ConfiguredRetriesLogEveryFailureIncludingLast(int retryCount)
    {
        var attempts=0;
        var logs=new List<string>();
        await Assert.ThrowsAsync<HttpRequestException>(()=>ModelRequest.RunAsync<string>(_=>{
            attempts++; throw new HttpRequestException("private body");
        },logs.Add,CancellationToken.None,(_,_)=>Task.CompletedTask,retryCount));
        Assert.Equal(retryCount+1,attempts);
        Assert.Equal(attempts,logs.Count(x=>x.Contains("HttpRequestException")));
        Assert.Contains($"{attempts}/{attempts}",logs.Last());
        Assert.All(logs,x=>Assert.DoesNotContain("private body",x));
    }

    [Fact]
    public async Task TimedOutAttemptCanRetryAndSucceed()
    {
        var attempts=0;
        var result=await ModelRequest.RunAsync(token=>ModelDeadline.RunAsync(async inner=>{
            if(++attempts==1) await Task.Delay(Timeout.Infinite,inner);
            return "ok";
        },TimeSpan.FromMilliseconds(50),token),_=>{},CancellationToken.None,(_,_)=>Task.CompletedTask,1);
        Assert.Equal("ok",result);
        Assert.Equal(2,attempts);
    }

    [Fact]
    public async Task CallerCancellationStopsRetries()
    {
        using var cancellation=new CancellationTokenSource();
        var attempts=0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ModelRequest.RunAsync<string>(_=>{
            attempts++; throw new HttpRequestException();
        },_=>{},cancellation.Token,(_,token)=>{cancellation.Cancel(); return Task.FromCanceled(token);}));
        Assert.Equal(1,attempts);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task InvalidRetryCountsAreRejectedBeforeCallingModel(int retryCount)
    {
        await Assert.ThrowsAsync<ArgumentException>(()=>ModelRequest.RunAsync<string>(_=>throw new InvalidOperationException(),_=>{},CancellationToken.None,retryCount:retryCount));
        Assert.Throws<ArgumentException>(()=>new ReviewProfile {ModelRetryCount=retryCount}.Validate());
    }

    [Fact]
    public void HistoricalProfilesDefaultToThreeRetries()
    {
        Assert.Equal(3,System.Text.Json.JsonSerializer.Deserialize<ReviewProfile>("{}")!.ModelRetryCount);
    }
}


