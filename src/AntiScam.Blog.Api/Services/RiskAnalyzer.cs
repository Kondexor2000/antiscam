using System.Text.RegularExpressions;
using AntiScam.Blog.Api.Models;

namespace AntiScam.Blog.Api.Services;

public sealed partial class RiskAnalyzer : IRiskAnalyzer
{
    private static readonly HashSet<string> TrustedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "google.com",
        "facebook.com",
        "messenger.com",
        "microsoft.com",
        "apple.com",
        "amazon.com",
        "paypal.com",
        "chatgpt.com"
    };

    private static readonly IReadOnlyDictionary<string, int> KeywordWeights = new Dictionary<string, int>
    {
        ["blik"] = 10,
        ["kod"] = 5,
        ["przelew"] = 7,
        ["pozycz"] = 8,
        ["pożycz"] = 8,
        ["szybko"] = 4,
        ["pilnie"] = 6,
        ["natychmiast"] = 6,
        ["wyslij"] = 5,
        ["wyślij"] = 5,
        ["potwierdz"] = 5,
        ["potwierdź"] = 5,
        ["bank"] = 3,
        ["konto"] = 3,
        ["zablokowane"] = 7,
        ["haslo"] = 5,
        ["hasło"] = 5
    };

    private static readonly string[] SafeSignals =
    [
        "dziekuje",
        "dziękuję",
        "spotkanie",
        "normalnie",
        "ok",
        "czesc",
        "cześć",
        "hej"
    ];

    private static readonly string[] IntentSignals =
    [
        "ostatnia szansa",
        "konto zablokowane",
        "natychmiast",
        "kliknij teraz",
        "potwierdz dane",
        "potwierdź dane"
    ];

    public RiskAssessment Analyze(BlogPostInput input)
    {
        var text = $"{input.Title} {input.Summary} {input.Content} {input.Author}";
        var textLow = text.ToLowerInvariant();
        var riskScore = 0;
        var reasons = new List<string>();

        var blikNumbers = BlikPattern().Matches(text)
            .Select(match => match.Value)
            .ToArray();
        if (blikNumbers.Length > 0)
        {
            var hasBlikContext = ContainsAny(textLow, ["blik", "kod", "przelew"]);
            riskScore += hasBlikContext ? 60 : 30;
            reasons.Add(hasBlikContext
                ? $"BLIK CONFIRMED: {string.Join(", ", blikNumbers)}"
                : $"BLIK UNCERTAIN: {string.Join(", ", blikNumbers)}");
        }

        var links = UrlPattern().Matches(text)
            .Select(match => match.Value)
            .ToArray();
        var safeLinks = new List<string>();
        var riskyLinks = new List<string>();

        foreach (var link in links)
        {
            var domain = ExtractDomain(link);
            if (IsTrustedDomain(domain))
            {
                safeLinks.Add(link);
            }
            else
            {
                riskyLinks.Add(link);
            }
        }

        if (riskyLinks.Count > 0)
        {
            riskScore += Math.Min(45, 20 + riskyLinks.Count * 10);
            reasons.Add($"Risky links: {string.Join(", ", riskyLinks)}");
        }

        if (safeLinks.Count > 0)
        {
            riskScore -= Math.Min(15, safeLinks.Count * 5);
            reasons.Add($"Trusted links: {string.Join(", ", safeLinks)}");
        }

        var keywordScore = KeywordWeights.Sum(pair => Math.Min(CountOccurrences(textLow, pair.Key), 2) * pair.Value);
        keywordScore = Math.Min(keywordScore, 30);
        if (keywordScore > 0)
        {
            riskScore += keywordScore;
            reasons.Add($"Keyword score: {keywordScore}");
        }

        var safeSignalHits = SafeSignals.Count(signal => textLow.Contains(signal, StringComparison.Ordinal));
        if (safeSignalHits >= 2)
        {
            riskScore -= 10;
            reasons.Add("Low-risk context detected");
        }

        if (IntentSignals.Any(signal => textLow.Contains(signal, StringComparison.Ordinal)))
        {
            riskScore += 10;
            reasons.Add("Scam intent detected");
        }

        riskScore = Math.Clamp(riskScore, 0, 100);
        var status = riskScore >= 80
            ? RiskStatuses.HighRisk
            : riskScore >= 50
                ? RiskStatuses.MediumRisk
                : RiskStatuses.LowRisk;

        return new RiskAssessment(status, riskScore, reasons, safeLinks, riskyLinks);
    }

    private static string ExtractDomain(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            ? parsed.Host.ToLowerInvariant().Replace("www.", "")
            : string.Empty;
    }

    private static bool IsTrustedDomain(string domain)
    {
        return TrustedDomains.Any(trusted => domain == trusted || domain.EndsWith($".{trusted}", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string text, IEnumerable<string> values)
    {
        return values.Any(value => text.Contains(value, StringComparison.Ordinal));
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex BlikPattern();

    [GeneratedRegex(@"https?://(?:[-\w.]|(?:%[\da-fA-F]{2}))+[^\s]*")]
    private static partial Regex UrlPattern();
}
