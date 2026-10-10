using CodeReviewer.Web.Services;
namespace CodeReviewer.Web.Tests;
public sealed class ReviewCommentTests
{
    private static ReviewRun Run(IReadOnlyList<FileAssessment>? coverage=null)=>new(Guid.NewGuid(),new(Guid.NewGuid(),"23",4,"MR","sha","https://example.test/mr/4","user"),"model","ConnectedModel","Completed",DateTime.UtcNow,DateTime.UtcNow,1,"scope",[],new ReviewProfile(),coverage);
    [Fact] public void EmptyLegacyResultCannotProduceAMeaninglessComment() {
        Assert.False(ReviewCommentFormatter.HasUsefulContent(Run()));
        Assert.Throws<InvalidOperationException>(()=>ReviewCommentFormatter.Build(Run()));
    }
    [Fact] public void ZeroDefectsCanHaveImpactAndTestsWithoutInternalNarration() {
        var run=Run([new("Validator.cs","بررسی شد",3,1,"INTERNAL_CHANGE","INTERNAL_ANALYSIS","INTERNAL_CHECK",true,2,0)]) with {
            Impact=new([new("Create item","src/Validator.cs")],[new("Viewer","configured relationship")],[new("Submit empty input","Validation shown","Validator.cs",1,"NotEmpty()","/items/new")],[],"High",true)
        };
        var body=ReviewCommentFormatter.Build(run);
        Assert.Contains("ایراد مهمی شناسایی نشد",body); Assert.Contains("Create item",body); Assert.Contains("Submit empty input",body);
        Assert.Contains("ریسک رگرسیون: زیاد",body); Assert.Contains("تست دستی UI: بله",body);
        Assert.DoesNotContain("INTERNAL",body); Assert.DoesNotContain("پیشنهاد مدل از معیارهای",body); Assert.DoesNotContain("خط افزوده",body);
    }
    [Fact] public void RuntimeCoverageSkipsGeneratedAndDocumentationWithoutLegacyFileCap() {
        var text="@@ -0,0 +1 @@\n+x";
        var coverage=ReviewCoverage.Create([new("a.cs",text,false,false),new("b.cs",text,false,false),new("c.js",text,false,false),new("d.cs",text,true,false),new("docs/spec.md",text,false,false)],new ReviewProfile {MaxFiles=1});
        Assert.All(coverage.Take(3),f=>Assert.Equal("بررسی نشده",f.Status));
        Assert.Contains("تولیدشده",coverage[3].Status); Assert.Contains("مستندات",coverage[4].Status);
    }
}
