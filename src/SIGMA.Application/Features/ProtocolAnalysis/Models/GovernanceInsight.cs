namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public sealed record GovernanceInsight
{
    public string Severity { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
