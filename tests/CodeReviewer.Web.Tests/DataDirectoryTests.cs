using CodeReviewer.Web.Services;
using Microsoft.Extensions.Configuration;

namespace CodeReviewer.Web.Tests;

public class DataDirectoryTests
{
    [Fact]
    public void UsesConfiguredDirectoryForStorage()
    {
        var path=Path.Combine(Path.GetTempPath(),"CodeReviewerConfigured");
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Storage:DataDirectory"]=path
        }).Build();
        Assert.Equal(Path.GetFullPath(path),DataDirectory.Resolve(config));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("data")]
    public void RequiresExplicitAbsoluteDirectoryInsteadOfUserProfileFallback(string? path)
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Storage:DataDirectory"]=path
        }).Build();
        Assert.Contains("Storage:DataDirectory",Assert.Throws<InvalidOperationException>(()=>DataDirectory.Resolve(config)).Message);
    }
}
