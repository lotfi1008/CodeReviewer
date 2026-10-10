namespace CodeReviewer.Web.Services;

public static class ModelDeadline
{
    public static async Task<T> RunAsync<T>(Func<CancellationToken,Task<T>> operation,TimeSpan limit,CancellationToken cancellationToken=default)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(limit);
        var task=operation(deadline.Token);
        try { return await task.WaitAsync(deadline.Token); }
        catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested)
        {
            Observe(task);
            throw new TimeoutException($"مدل طی {limit.TotalSeconds:F0} ثانیه پاسخ کامل نداد. اجرا متوقف شد؛ مدل دیگری یا پروفایل سبک‌تر انتخاب کنید و دوباره اجرا کنید.");
        }
        catch(OperationCanceledException) { Observe(task); throw; }
    }
    private static void Observe<T>(Task<T> task) => _=task.ContinueWith(t=>{ _=t.Exception; },CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
}
