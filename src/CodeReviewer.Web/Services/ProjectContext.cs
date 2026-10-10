using System.Text.Json;

namespace CodeReviewer.Web.Services;

public sealed class ProjectContext
{
    public string Name { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string Architecture { get; set; } = "";
    public string Technologies { get; set; } = "";
    public string Boundaries { get; set; } = "";
    public List<ProjectFeature> Features { get; set; } = [];
    public void Validate()
    {
        if(Features is null || Features.Any(f=>f is null || f.PathPrefixes is null || f.RelatedFeatures is null || f.Scenarios is null || f.UiPath is null)) throw new ArgumentException("ساختار قابلیت‌ها معتبر نیست.");
        if(JsonSerializer.Serialize(this).Length>24000 || Features.Count>60) throw new ArgumentException("نقشه باید مختصر باشد: حداکثر ۶۰ قابلیت و ۲۴ هزار کاراکتر.");
        if(Features.Any(f=>string.IsNullOrWhiteSpace(f.Name) || f.PathPrefixes.Count==0 || f.PathPrefixes.Any(p=>string.IsNullOrWhiteSpace(p) || p.Contains("..") || p.StartsWith('/')))
            || Features.Select(f=>f.Name).Distinct().Count()!=Features.Count) throw new ArgumentException("هر قابلیت نام یکتا و مسیر نسبی معتبر لازم دارد.");
        if(Features.Any(f=>f.RelatedFeatures.Any(n=>!Features.Any(x=>x.Name==n)))) throw new ArgumentException("نام قابلیت مرتبط باید در همین نقشه موجود باشد.");
        if(Features.Any(f=>f.Scenarios.Any(s=>string.IsNullOrWhiteSpace(s.Steps) || string.IsNullOrWhiteSpace(s.Expected)))) throw new ArgumentException("هر سناریو مراحل و نتیجهٔ مورد انتظار لازم دارد.");
    }
    public ProjectContext ForFile(string path) => new() {Name=Name,Purpose=Purpose,Architecture=Architecture,Technologies=Technologies,Boundaries=Boundaries,
        Features=Features.Where(f=>f.Matches(path)).Select(f=>new ProjectFeature {Name=f.Name,UiPath=f.UiPath,Critical=f.Critical,RelatedFeatures=f.RelatedFeatures}).ToList()};
}
public sealed class ProjectFeature
{
    public string Name {get;set;}="";
    public List<string> PathPrefixes {get;set;}=[];
    public string UiPath {get;set;}="";
    public bool Critical {get;set;}
    public List<string> RelatedFeatures {get;set;}=[];
    public List<RegressionScenario> Scenarios {get;set;}=[];
    public bool Matches(string path)=>PathPrefixes.Any(p=>path.Replace('\\','/').StartsWith(p.Replace('\\','/'),StringComparison.Ordinal));
}
public sealed record RegressionScenario(string Steps,string Expected,string File="",int Line=0,string Evidence="",string UiPath="");
public sealed record FeatureImpact(string Feature,string Evidence);
public sealed record ChangeImpactResult(List<FeatureImpact> Direct,List<FeatureImpact> Possible,List<RegressionScenario> ManualTests,
    List<string> RelatedTests,string Risk,bool ContextConfigured,int UnmappedFiles=0);

public sealed class ProjectContextStore(SqlDataStore database)
{
    public ProjectContext Read(Guid source,string repo)=>database.ReadSnapshot<ProjectContext>(source,"project-context:"+repo) ?? new();
    public void Save(Guid source,string repo,ProjectContext context) { context.Validate(); database.SaveSnapshot(source,"project-context:"+repo,context); }
}
