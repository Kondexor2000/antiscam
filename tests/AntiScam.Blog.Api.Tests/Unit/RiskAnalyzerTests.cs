using AntiScam.Blog.Api.Models;
using AntiScam.Blog.Api.Services;

namespace AntiScam.Blog.Api.Tests.Unit;

public sealed class RiskAnalyzerTests
{
    [Fact]
    public void Analyze_AllowsLowRiskBlogPost()
    {
        var analyzer = new RiskAnalyzer();
        var input = new BlogPostInput(
            "Bezpieczne spotkanie",
            "Normalny wpis edukacyjny.",
            "Cześć, to spokojna informacja o spotkaniu i bezpiecznym zachowaniu online.",
            "AntiScam Team");

        var risk = analyzer.Analyze(input);

        Assert.Equal(RiskStatuses.LowRisk, risk.Status);
        Assert.True(risk.CanPublish);
    }

    [Fact]
    public void Analyze_BlocksBlikScamMessage()
    {
        var analyzer = new RiskAnalyzer();
        var input = new BlogPostInput(
            "Pilnie wyślij BLIK",
            "Konto zablokowane.",
            "Wyślij kod BLIK 123456 natychmiast i potwierdź dane.",
            "Tester");

        var risk = analyzer.Analyze(input);

        Assert.Equal(RiskStatuses.HighRisk, risk.Status);
        Assert.False(risk.CanPublish);
        Assert.Contains("zablokowana", risk.BlockExplanation);
        Assert.Contains(RiskStatuses.HighRisk, risk.BlockExplanation);
        Assert.Contains(risk.Reasons, reason => reason.StartsWith("BLIK CONFIRMED"));
    }
}
