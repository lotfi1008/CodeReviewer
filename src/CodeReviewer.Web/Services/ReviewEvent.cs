namespace CodeReviewer.Web.Services;

public sealed record ReviewEvent(long Id, DateTime TimeUtc, string Level, string Message, string File, int CompletedFiles, int TotalFiles);
public sealed record ReviewLog(string Status, DateTime StartedUtc, DateTime? FinishedUtc, IReadOnlyList<ReviewEvent> Events);
