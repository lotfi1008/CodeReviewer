namespace CodeReviewer.Web.Services;

public static class ChangeImpactAnalyzer
{
    public static ChangeImpactResult Analyze(IReadOnlyList<ChangedFile> files,ProjectContext context,ReviewProfile profile)
    {
        var relevant=files.Where(f=>ReviewScope.SkipReason(f,profile) is null && !ReviewScope.Documentation(f.Path)).ToArray();
        var matched=context.Features.Where(f=>relevant.Any(x=>f.Matches(x.Path))).ToArray();
        var direct=matched.Select(f=>new FeatureImpact(f.Name,string.Join(", ",relevant.Where(x=>f.Matches(x.Path)).Select(x=>x.Path)))).ToList();
        var indirect=matched.SelectMany(f=>f.RelatedFeatures.Select(name=>new FeatureImpact(name,$"رابطهٔ ثبت‌شده با {f.Name}؛ مسیر فراخوانی تأیید نشده است.")))
            .Where(f=>direct.All(d=>d.Feature!=f.Feature)).DistinctBy(f=>f.Feature).ToList();
        var scenarios=matched.SelectMany(f=>f.Scenarios.Select(s=>s with {File=relevant.First(x=>f.Matches(x.Path)).Path,UiPath=f.UiPath})).DistinctBy(s=>(s.Steps,s.Expected)).ToList();
        var tests=relevant.Where(f=>f.Path.Contains("test",StringComparison.OrdinalIgnoreCase)).Select(f=>f.Path).ToList();
        return new(direct,indirect,scenarios,tests,matched.Any(f=>f.Critical) ? "High" : relevant.Length>0 ? "Medium" : "Low",context.Features.Count>0,relevant.Count(f=>!matched.Any(x=>x.Matches(f.Path))));
    }
    public static bool AcceptScenario(RegressionScenario s,ChangedFile file,DiffChunk chunk,ProjectContext context)
    {
        if(s is null || s.UiPath is null || s.File!=file.Path || !chunk.AddedLines.Contains(s.Line) || string.IsNullOrWhiteSpace(s.Steps) || string.IsNullOrWhiteSpace(s.Expected) || string.IsNullOrWhiteSpace(s.Evidence) || s.Evidence.Trim().Length<4 || s.Steps.Length>800 || s.Expected.Length>500) return false;
        if(!DiffDocument.Parse(file.Diff).Any(l=>l.Kind=="added" && l.NewLine==s.Line && l.Text.Contains(s.Evidence,StringComparison.Ordinal))) return false;
        return string.IsNullOrEmpty(s.UiPath) || context.Features.Any(f=>f.Matches(file.Path) && f.UiPath==s.UiPath);
    }
}
