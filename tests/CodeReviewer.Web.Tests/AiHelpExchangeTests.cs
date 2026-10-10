using System.Text.Json;
using CodeReviewer.Web.Services;

namespace CodeReviewer.Web.Tests;

public sealed class AiHelpExchangeTests
{
    [Fact]
    public void ParsesCurrentConversationEntry()
    {
        var text=JsonSerializer.Serialize(new AiHelpExchange("پرسش اول؟", "پاسخ\nچندخطی", "مدل آزمایشی"));

        var exchange=ReviewWorkspaceService.ParseAiHelp(text);

        Assert.NotNull(exchange);
        Assert.Equal("پرسش اول؟",exchange.Question);
        Assert.Equal("پاسخ\nچندخطی",exchange.Answer);
        Assert.Equal("مدل آزمایشی",exchange.Model);
    }

    [Fact]
    public void ParsesHistoricalConversationEntry()
    {
        var exchange=ReviewWorkspaceService.ParseAiHelp("پرسش: آیا امن است؟\n\nپاسخ (internal / model):\nبله، با شرط بررسی ورودی.");

        Assert.NotNull(exchange);
        Assert.Equal("آیا امن است؟",exchange.Question);
        Assert.Equal("بله، با شرط بررسی ورودی.",exchange.Answer);
        Assert.Equal("internal / model",exchange.Model);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid json")]
    [InlineData("پاسخ بدون پرسش")]
    public void IgnoresMalformedConversationEntry(string text)
    {
        Assert.Null(ReviewWorkspaceService.ParseAiHelp(text));
    }
}
