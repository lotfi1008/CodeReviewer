using CodeReviewer.Web.Services;
namespace CodeReviewer.Web.Tests;

public sealed class ModelDeadlineTests
{
    [Fact]
    public async Task StopsWaitingEvenIfProviderIgnoresCancellation()
    {
        var provider=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task=ModelDeadline.RunAsync(_=>provider.Task,TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<TimeoutException>(()=>task.WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.False(provider.Task.IsCompleted);
        provider.SetResult("late response");
    }
    [Fact]
    public async Task UserCancellationIsNotReportedAsTimeout()
    {
        using var cancelled=new CancellationTokenSource();
        var provider=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var task=ModelDeadline.RunAsync(_=>provider.Task,TimeSpan.FromSeconds(30),cancelled.Token);
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>task.WaitAsync(TimeSpan.FromSeconds(3)));
        provider.SetResult("late response");
    }
}
