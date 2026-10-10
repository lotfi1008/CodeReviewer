using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace CodeReviewer.Web.Services;

public sealed record ReviewJob(Guid RunId,MergeRequestItem Request,Guid ModelId,ReviewProfile Profile,bool PublishToMergeRequest=false);

public sealed class ReviewJobQueue(IConfiguration config,SqlDataStore database,ConnectionCatalogStore catalog)
{
    private SqlConnection Open() { var c=new SqlConnection(config.GetConnectionString("CodeReviewer")); c.Open(); return c; }
    public void Initialize()
    {
        using var c=Open(); using var q=c.CreateCommand();
        q.CommandText="""
            IF OBJECT_ID('dbo.ReviewJobs') IS NULL CREATE TABLE dbo.ReviewJobs(
                RunId uniqueidentifier NOT NULL PRIMARY KEY, Payload nvarchar(max) NOT NULL,
                State nvarchar(20) NOT NULL, CancelRequested bit NOT NULL DEFAULT 0, CreatedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME());
            UPDATE j SET State='Completed' FROM dbo.ReviewJobs j JOIN dbo.ReviewRuns r ON r.Id=j.RunId WHERE j.State IN ('Queued','Running') AND r.Status='Completed';
            UPDATE dbo.ReviewJobs SET State='Queued' WHERE State='Running';
            UPDATE r SET Status='Running',FinishedUtc=NULL FROM dbo.ReviewRuns r JOIN dbo.ReviewJobs j ON j.RunId=r.Id WHERE j.State='Queued';
            """;
        q.ExecuteNonQuery();
    }
    public Guid Enqueue(MergeRequestItem mr,Guid modelId,Guid profileId,bool publishToMergeRequest=false)
    {
        var profile=database.Profiles().Single(p=>p.Id==profileId); profile.Validate(); profile.ModelId=modelId;
        var model=catalog.Snapshot().Models.Single(m=>m.Id==modelId && m.Enabled);
        foreach(var id in profile.Domains.Where(d=>d.ModelId.HasValue).Select(d=>d.ModelId!.Value))
            if(!catalog.Snapshot().Models.Any(m=>m.Id==id && m.Enabled)) throw new ArgumentException("مدل یکی از حوزه‌ها غیرفعال یا حذف شده است.");
        var run=database.StartReview(mr,model.Name+" / "+model.Model,profile:profile);
        try
        {
            using var c=Open(); using var q=c.CreateCommand();
            q.CommandText="INSERT dbo.ReviewJobs (RunId,Payload,State) VALUES (@id,@payload,'Queued')";
            q.Parameters.AddWithValue("@id",run); q.Parameters.AddWithValue("@payload",JsonSerializer.Serialize(new ReviewJob(run,mr,modelId,profile,publishToMergeRequest))); q.ExecuteNonQuery();
            database.AppendReviewEvent(run,"در صف پردازش سرور؛ می‌توانید صفحه را ببندید. نتیجه و لاگ در سوابق در دسترس است."); return run;
        }
        catch { database.FinishReview(run,"Failed",0,[],"ثبت در صف SQL ناموفق بود."); throw; }
    }
    public ReviewJob? Claim()
    {
        using var c=Open(); using var q=c.CreateCommand();
        q.CommandText=";WITH job AS (SELECT TOP(1) * FROM dbo.ReviewJobs WITH (UPDLOCK,READPAST,ROWLOCK) WHERE State='Queued' ORDER BY CreatedUtc) UPDATE job SET State='Running' OUTPUT inserted.Payload;";
        return q.ExecuteScalar() is string json ? JsonSerializer.Deserialize<ReviewJob>(json) : null;
    }
    public void Cancel(Guid id) { using var c=Open(); using var q=c.CreateCommand(); q.CommandText="UPDATE dbo.ReviewJobs SET CancelRequested=1 WHERE RunId=@id AND State IN ('Queued','Running')"; q.Parameters.AddWithValue("@id",id); q.ExecuteNonQuery(); }
    public bool IsCancelled(Guid id) { using var c=Open(); using var q=c.CreateCommand(); q.CommandText="SELECT CancelRequested FROM dbo.ReviewJobs WHERE RunId=@id"; q.Parameters.AddWithValue("@id",id); return q.ExecuteScalar() is true; }
    public void Finish(Guid id,bool requeue)
    {
        using var c=Open(); using var q=c.CreateCommand();
        q.CommandText=requeue ? "UPDATE j SET State=CASE WHEN r.Status='Completed' THEN 'Completed' ELSE 'Queued' END FROM dbo.ReviewJobs j JOIN dbo.ReviewRuns r ON r.Id=j.RunId WHERE j.RunId=@id; UPDATE dbo.ReviewRuns SET Status='Running',FinishedUtc=NULL WHERE Id=@id AND Status<>'Completed'" : "UPDATE dbo.ReviewJobs SET State='Completed' WHERE RunId=@id";
        q.Parameters.AddWithValue("@id",id); q.ExecuteNonQuery();
    }
}

public sealed class ReviewWorker(ReviewJobQueue queue,IServiceScopeFactory scopes,SqlDataStore database,ILogger<ReviewWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            ReviewJob? job=null;
            try
            {
                job=queue.Claim();
                if(job is null) { await Task.Delay(2000,stoppingToken); continue; }
                using var scope=scopes.CreateScope(); using var cancel=CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                if(queue.IsCancelled(job.RunId)) cancel.Cancel();
                var operation=scope.ServiceProvider.GetRequiredService<AiReviewService>().ReviewAsync(job.Request,job.ModelId,cancel.Token,existingRunId:job.RunId,frozenProfile:job.Profile,publishToMergeRequest:job.PublishToMergeRequest);
                try
                {
                    while(!operation.IsCompleted) {
                        await Task.WhenAny(operation,Task.Delay(1000,stoppingToken));
                        if(stoppingToken.IsCancellationRequested) { cancel.Cancel(); await operation; break; }
                        if(queue.IsCancelled(job.RunId)) cancel.Cancel();
                    }
                    await operation;
                }
                catch
                {
                    // Never dispose the scope while an unobserved review is still running.
                    cancel.Cancel();
                    try { await operation; } catch { }
                    throw;
                }
                queue.Finish(job.RunId,false);
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { if(job is not null) queue.Finish(job.RunId,true); break; }
            catch(SqlException)
            {
                logger.LogError("SQL unavailable; the worker will retry after recovery.");
                while(!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(5000,stoppingToken);
                        if(job is not null) queue.Finish(job.RunId,true);
                        break;
                    }
                    catch(SqlException) { }
                    catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
                }
            }
            catch(Exception ex)
            {
                logger.LogError("Review job failed: {Type}",ex.GetType().Name);
                if(job is not null) {
                    if(database.ReadReviewLog(job.RunId)?.Status=="Running") database.FinishReview(job.RunId,"Failed",0,[],"پردازش کامل نشد؛ لاگ و گزارش پوشش را بررسی کنید.");
                    queue.Finish(job.RunId,false);
                }
                else { try { await Task.Delay(5000,stoppingToken); } catch(OperationCanceledException) { break; } }
            }
        }
    }
}

