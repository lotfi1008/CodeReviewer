namespace CodeReviewer.Web.Services;

public static class FindingQuality
{
    public static bool HasEvidence(ReviewFinding f)=>!f.ObviousAdvice && !string.IsNullOrWhiteSpace(f.Evidence)
        && !string.IsNullOrWhiteSpace(f.Impact) && !string.IsNullOrWhiteSpace(f.ChangeReason);
    public static bool Accept(ReviewFinding f,ChangedFile file,DiffChunk chunk,ReviewProfile profile,IReadOnlyList<ReviewDomain> domains)
    {
        if(f is null || f.File!=file.Path || !chunk.AddedLines.Contains(f.Line) || !profile.Accepts(f) || !domains.Any(d=>d.Key==f.Category)
            || !HasEvidence(f) || string.IsNullOrWhiteSpace(f.Title) || string.IsNullOrWhiteSpace(f.Description) || string.IsNullOrWhiteSpace(f.Suggestion)
            || f.Title.Length>180 || f.Description.Length>1000 || f.Impact.Length>500 || f.Suggestion.Length>700) return false;
        return DiffDocument.Parse(file.Diff).Any(l=>l.Kind=="added" && l.NewLine==f.Line && f.Evidence.Trim().Length>=4 && l.Text.Contains(f.Evidence.Trim(),StringComparison.Ordinal));
    }
}
