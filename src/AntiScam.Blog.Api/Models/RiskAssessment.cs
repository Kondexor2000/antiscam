namespace AntiScam.Blog.Api.Models;

public sealed record RiskAssessment(
    string Status,
    int RiskScore,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> SafeLinks,
    IReadOnlyList<string> RiskyLinks)
{
    public bool CanPublish => Status == RiskStatuses.LowRisk;
}
