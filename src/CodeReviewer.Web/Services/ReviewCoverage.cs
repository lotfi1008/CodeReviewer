namespace CodeReviewer.Web.Services;

public sealed record FileAssessment(string File, string Status, int AddedLines, int RemovedLines,
    string ChangeSummary = "", string Assessment = "", string HumanCheck = "", bool Truncated = false,
    int ReturnedFindings = 0, int AcceptedFindings = 0, int CompletedParts = 0, int TotalParts = 0);

public static class ReviewCoverage
{
    public static List<FileAssessment> Create(IReadOnlyList<ChangedFile> files, ReviewProfile profile)
    {
        return files.Select(f =>
        {
            var status = ReviewScope.SkipReason(f,profile) ?? (f.TooLarge ? "Diff ناقص از سرویس" : string.IsNullOrWhiteSpace(f.Diff) ? "Diff متنی در دسترس نیست" : ReviewScope.Route(f,profile).Count==0 ? "حوزهٔ مرتبط فعال نیست" : "بررسی نشده");
            return new FileAssessment(f.Path,status,
                f.Diff.Split('\n').Count(l=>l.StartsWith('+') && !l.StartsWith("+++")),
                f.Diff.Split('\n').Count(l=>l.StartsWith('-') && !l.StartsWith("---")));
        }).ToList();
    }
}


