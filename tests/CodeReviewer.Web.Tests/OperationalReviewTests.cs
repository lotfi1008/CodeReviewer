using CodeReviewer.Web.Services;

namespace CodeReviewer.Web.Tests;

public sealed class OperationalReviewTests
{
    [Fact]
    public void ChunksCoverEveryAddedLineWithoutLosingTailOrLongLines()
    {
        var longLine=new string('x',4000);
        var diff="@@ -0,0 +1,3 @@\n+first\n+"+longLine+"\n+last";
        var chunks=DiffChunker.Split(diff,1000);
        Assert.True(chunks.Count>1);
        Assert.All(chunks,c=>Assert.True(c.Text.Length<=1000));
        Assert.Equal(new[]{1,2,3},chunks.SelectMany(c=>c.AddedLines).Distinct().Order().ToArray());
        Assert.Equal(4000,chunks.Sum(c=>c.Text.Count(ch=>ch=='x')));
        Assert.Contains("last",chunks.Last().Text);
    }
    [Fact]
    public void BusinessReviewRequiresExplicitRulesAndCanUseSeparateModel()
    {
        var p=new ReviewProfile {Domains=[new(){Key="security"},new(){Key="business",ModelId=Guid.NewGuid()}]};
        Assert.Throws<ArgumentException>(()=>p.Validate());
        p.BusinessRules="فایل خالی باید پیش از ذخیره رد شود."; p.Validate();
        p.Domains.Add(new(){Key="security"}); Assert.Throws<ArgumentException>(()=>p.Validate());
    }
}
