namespace CodeReviewer.Web.Services;

public static class ReviewDecision
{
    public static bool NeedsChanges(ReviewRun run) => ReviewCommentFormatter.Important(run).Any(f =>
        ReviewProfile.SeverityRank(f.Severity) >= ReviewProfile.SeverityRank(run.Profile?.BlockingSeverity ?? "medium"));
    public static string Title(ReviewRun run) => run.Status == "Running"
        ? (NeedsChanges(run) ? "در حال بررسی؛ نیازمند اصلاح" : "در حال بررسی")
        : run.Status != "Completed" ? (NeedsChanges(run) ? "بازبینی ناقص؛ نیازمند اصلاح" : "بازبینی ناقص")
        : NeedsChanges(run) ? "نیازمند اصلاح" : "بررسی شد؛ بدون ایراد مسدودکننده";
    public static string CommitState(ReviewRun run) => NeedsChanges(run) ? "failed"
        : run.Status == "Running" ? "running" : run.Status == "Completed" ? "success" : "failed";
    public static string Context(ReviewRun run) => $"code-reviewer/mr-{run.MergeRequest.Number}/{run.Profile?.Id.ToString() ?? "default"}";
    public static string LabelPrefix(ReviewRun run) => $"بازبین-{(run.Profile?.Id.ToString("N") ?? "default")}::";
    public static string Label(ReviewRun run) => LabelPrefix(run) + (NeedsChanges(run) ? "نیازمند اصلاح" : run.Status=="Running" ? "در حال بررسی" : run.Status=="Completed" ? "بررسی شد" : "ناقص");
}


