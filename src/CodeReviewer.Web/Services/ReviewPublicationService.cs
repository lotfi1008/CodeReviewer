namespace CodeReviewer.Web.Services;

public sealed class ReviewPublicationService(SourceControlService source, SqlDataStore database)
{
    // This application runs as one host. Serialize retries and automatic/manual publication.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<string> PublishAsync(Guid runId, CancellationToken ct = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromMinutes(2));
        await Gate.WaitAsync(deadline.Token);
        try
        {
            var run = database.History(id:runId).Single();
            if (run.Status is not ("Running" or "Completed" or "Incomplete" or "Failed" or "TimedOut" or "Cancelled")) throw new InvalidOperationException("وضعیت این بازبینی قابل انتشار نیست.");
            if(run.Status=="Completed" && !ReviewCommentFormatter.HasUsefulContent(run))
            {
                const string reason="کامنت منتشر نشد: این اجرا یافته یا توضیح مستند دربارهٔ کد ندارد. برای گزارش کاربردی، بازبینی را دوباره اجرا کنید.";
                database.SaveSnapshot(run.MergeRequest.ConnectionId,$"publication:{runId}",new {Status="Skipped",Reason=reason,TimeUtc=DateTime.UtcNow});
                database.AppendReviewEvent(runId,reason,"Warning",completed:run.FilesReviewed,total:run.Coverage?.Count ?? run.FilesReviewed);
                throw new InvalidOperationException(reason);
            }
            database.AppendReviewEvent(runId, "در حال انتشار نتیجه روی مرج ریکوست…", completed:run.FilesReviewed,total:run.Coverage?.Count ?? run.FilesReviewed);
            try
            {
                var url = await source.PublishReviewAsync(run, deadline.Token);
                database.SaveSnapshot(run.MergeRequest.ConnectionId, $"publication:{runId}", new { Status="Published", Url=url, Decision=ReviewDecision.Title(run), CommitState=ReviewDecision.CommitState(run), PublishedUtc=DateTime.UtcNow });
                database.AppendReviewEvent(runId, $"کامنت نتیجه منتشر شد: {url}", completed:run.FilesReviewed,total:run.Coverage?.Count ?? run.FilesReviewed);
                return url;
            }
            catch (Exception ex)
            {
                var reason = ex is InvalidOperationException or NotSupportedException ? ex.Message : ex is OperationCanceledException ? "مهلت انتشار تمام شد یا عملیات لغو شد؛ وضعیت کامنت در تلاش بعدی بررسی می‌شود." : $"خطای ارتباط با سورس کنترل ({ex.GetType().Name}).";
                database.SaveSnapshot(run.MergeRequest.ConnectionId, $"publication:{runId}", new { Status="Failed", Reason=reason, TimeUtc=DateTime.UtcNow });
                database.AppendReviewEvent(runId, "انتشار کامل نشد؛ نتیجهٔ بازبینی ذخیره است. " + reason, "Error",completed:run.FilesReviewed,total:run.Coverage?.Count ?? run.FilesReviewed);
                throw new InvalidOperationException(reason, ex);
            }
        }
        finally { Gate.Release(); }
    }
}


