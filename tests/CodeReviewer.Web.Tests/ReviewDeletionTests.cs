using CodeReviewer.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CodeReviewer.Web.Tests;

// آزمون‌های حذف واقعی گزارش بازبینی: حذف موفق به‌همراه دادهٔ وابسته، رفتار Idempotent،
// محافظت از اجرای Running و دست‌نخورده‌ماندن دادهٔ اجراهای دیگر.
// با [SqlServerFact] در نبود SQL Server قابل‌استفاده، این آزمون‌ها به‌صورت پویا Skip می‌شوند.
public sealed class ReviewDeletionTests : IDisposable
{
    private sealed record SnapshotProbe(string Kind);

    private readonly string dbName = "CodeReviewerTest_" + Guid.NewGuid().ToString("N");
    private bool created;

    private string testConnection => SqlTestServer.WithDatabase(dbName);
    private string masterConnection => SqlTestServer.WithDatabase("master");

    private SqlDataStore CreateStore(IDataProtectionProvider provider)
    {
        if (!created)
        {
            using var connection = new SqlConnection(masterConnection); connection.Open();
            using var command = connection.CreateCommand(); command.CommandText = $"CREATE DATABASE [{dbName}]"; command.ExecuteNonQuery(); created = true;
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:CodeReviewer"] = testConnection }).Build();
        var database = new SqlDataStore(config, provider); database.Initialize();
        return database;
    }

    private int Scalar(string sql, Guid connectionId, Guid reviewId)
    {
        using var connection = new SqlConnection(testConnection); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = sql;
        command.Parameters.AddWithValue("@connection", connectionId);
        command.Parameters.AddWithValue("@id", reviewId);
        return (int)command.ExecuteScalar()!;
    }

    public void Dispose()
    {
        if (!created || !System.Text.RegularExpressions.Regex.IsMatch(dbName, @"^CodeReviewerTest_[0-9a-f]{32}$")) return;
        SqlConnection.ClearAllPools();
        using var connection = new SqlConnection(masterConnection); connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}]"; command.ExecuteNonQuery();
    }

    [SqlServerFact]
    public void DeleteReviewPurgesDependentDataAndIsIdempotent()
    {
        var provider = new EphemeralDataProtectionProvider();
        var database = CreateStore(provider);
        var connectionId = Guid.NewGuid();
        var mr = new MergeRequestItem(connectionId, "23", 4, "test request", "head", "https://example.test/mr/4", "author");

        var target = database.StartReview(mr, "test-model");
        database.FinishReview(target, "Completed", 1, [new("correctness", "medium", 0.95, "Example.cs", 10, "Defect", "Details", "Fix")]);
        database.AppendReviewEvent(target, "شروع بازبینی", "Info", "Example.cs", 1, 1);
        // Snapshotهای وابسته به اجرا (الگوی '{prefix}:{runId}') و یک Snapshot مشترک که باید بماند.
        database.SaveSnapshot(connectionId, $"impact:{target}", new SnapshotProbe("impact"));
        database.SaveSnapshot(connectionId, $"context:{target}", new SnapshotProbe("context"));
        database.SaveSnapshot(connectionId, "project-context", new SnapshotProbe("shared"));

        var other = database.StartReview(mr, "test-model");
        database.FinishReview(other, "Completed", 1, [new("style", "low", 0.5, "Other.cs", 3, "Style", "D", "S")]);

        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM dbo.Findings WHERE ReviewId=@id", connectionId, target));
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM dbo.ReviewEvents WHERE ReviewId=@id", connectionId, target));

        Assert.Equal(ReviewDeleteResult.Deleted, database.DeleteReview(target));

        // رکورد اصلی و همهٔ دادهٔ وابستهٔ آن حذف شده‌اند.
        Assert.Empty(database.History(id: target));
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM dbo.Findings WHERE ReviewId=@id", connectionId, target));
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM dbo.ReviewEvents WHERE ReviewId=@id", connectionId, target));
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM dbo.SourceSnapshots WHERE ConnectionId=@connection AND Scope LIKE '%:' + CONVERT(nvarchar(36),@id)", connectionId, target));

        // دادهٔ مشترک و اجرای دیگر دست‌نخورده مانده‌اند.
        Assert.Equal("shared", database.ReadSnapshot<SnapshotProbe>(connectionId, "project-context")?.Kind);
        Assert.Single(database.History(id: other));

        // حذف مجدد همان شناسه و شناسهٔ ناموجود هر دو Idempotent و بدون استثنا هستند.
        Assert.Equal(ReviewDeleteResult.NotFound, database.DeleteReview(target));
        Assert.Equal(ReviewDeleteResult.NotFound, database.DeleteReview(Guid.NewGuid()));
    }

    [SqlServerFact]
    public void DeleteReviewRejectsRunningRunAndLeavesOtherRunsIntact()
    {
        var provider = new EphemeralDataProtectionProvider();
        var database = CreateStore(provider);
        var connectionId = Guid.NewGuid();
        var mr = new MergeRequestItem(connectionId, "23", 4, "test request", "head", "https://example.test/mr/4", "author");

        var running = database.StartReview(mr, "test-model");
        database.AppendReviewEvent(running, "در حال اجرا", "Info", "Example.cs", 0, 3);

        var completed = database.StartReview(mr, "test-model");
        database.FinishReview(completed, "Completed", 1, [new("correctness", "medium", 0.95, "Example.cs", 1, "Defect", "D", "F")]);

        Assert.Equal(ReviewDeleteResult.Running, database.DeleteReview(running));

        // اجرای Running و رویداد آن سالم مانده‌اند.
        Assert.Equal("Running", Assert.Single(database.History(id: running)).Status);
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM dbo.ReviewEvents WHERE ReviewId=@id", connectionId, running));

        // اجرای دیگر نیز دست‌نخورده است و همچنان قابل حذف است.
        Assert.Single(database.History(id: completed));
        Assert.Equal(ReviewDeleteResult.Deleted, database.DeleteReview(completed));
    }
}
