using CodeReviewer.Web.Services;

namespace CodeReviewer.Web.Tests;
public class HighSignalReviewTests
{
    private static ChangedFile File(string path="src/Orders/Validator.cs")=>new(path,"@@ -1 +1 @@\n-RuleFor(x=>x.Name);\n+RuleFor(x=>x.Name).NotEmpty();",false,false);
    [Fact] public void FindingRequiresMatchingChangedLineConcreteImpactAndNonGenericAdvice()
    {
        var file=File(); var chunk=DiffChunker.Split(file.Diff,1000).Single(); var profile=new ReviewProfile();
        var finding=new ReviewFinding("correctness","medium",.95,file.Path,1,"Contract regression","Existing caller is rejected","Keep accepted input contract",".NotEmpty()","Caller request rejected","Empty input was accepted before");
        Assert.True(FindingQuality.Accept(finding,file,chunk,profile,profile.Domains));
        Assert.False(FindingQuality.Accept(finding with {Evidence="imaginary code"},file,chunk,profile,profile.Domains));
        Assert.False(FindingQuality.Accept(finding with {Impact=""},file,chunk,profile,profile.Domains));
        Assert.False(FindingQuality.Accept(finding with {ObviousAdvice=true},file,chunk,profile,profile.Domains));
        Assert.False(FindingQuality.Accept(finding with {Line=40},file,chunk,profile,profile.Domains));
        Assert.False(FindingQuality.Accept(finding with {Confidence=.5},file,chunk,profile,profile.Domains));
    }
    [Fact] public void ImpactUsesConfiguredPathsAndLabelsRelationshipsAsPossible()
    {
        var context=new ProjectContext {Name="Example",Features=[new(){Name="Orders",PathPrefixes=["src/Orders/"],UiPath="/orders",Critical=true,RelatedFeatures=["Viewer"],Scenarios=[new("Submit empty form","Validation shown")]},new(){Name="Viewer",PathPrefixes=["src/Viewer/"]}]};
        context.Validate(); var impact=ChangeImpactAnalyzer.Analyze([File()],context,new());
        Assert.Equal("Orders",Assert.Single(impact.Direct).Feature);
        Assert.Equal("Viewer",Assert.Single(impact.Possible).Feature);
        Assert.Equal("High",impact.Risk); Assert.Equal("/orders",Assert.Single(impact.ManualTests).UiPath);
        Assert.Empty(context.ForFile("src/Other.cs").Features);
    }
    [Fact] public void ManualTestsRequireEvidenceAndKnownUiPath()
    {
        var file=File(); var chunk=DiffChunker.Split(file.Diff,1000).Single();
        var context=new ProjectContext {Features=[new(){Name="Orders",PathPrefixes=["src/Orders/"],UiPath="/orders"}]};
        var scenario=new RegressionScenario("Submit blank name","Validation error",file.Path,1,"NotEmpty()","/orders");
        Assert.True(ChangeImpactAnalyzer.AcceptScenario(scenario,file,chunk,context));
        Assert.False(ChangeImpactAnalyzer.AcceptScenario(scenario with {UiPath="/invented"},file,chunk,context));
        Assert.False(ChangeImpactAnalyzer.AcceptScenario(scenario with {Evidence="invented"},file,chunk,context));
    }
    [Fact] public void RoutingAndFormattingRulesAvoidDroppingBehaviorChanges()
    {
        var profile=new ReviewProfile {Domains=[new(){Key="correctness"},new(){Key="database"}]};
        Assert.Single(ReviewScope.Route(File(),profile));
        Assert.NotNull(ReviewScope.SkipReason(File("docs/spec.md"),profile));
        profile.ReviewDocumentation=true; Assert.Null(ReviewScope.SkipReason(File("docs/spec.md"),profile));
        Assert.NotNull(ReviewScope.SkipReason(new("a.cs","@@ -1 +1 @@\n-  Foo();\n+Foo();",false,false),profile));
        Assert.Null(ReviewScope.SkipReason(new("a.cs","@@ -1 +1 @@\n-var s=\"a b\";\n+var s=\"ab\";",false,false),profile));
        Assert.Null(ReviewScope.SkipReason(new("a.py","@@ -1 +1 @@\n-  foo()\n+foo()",false,false),profile));
    }
}
