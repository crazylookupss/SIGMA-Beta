namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public enum GovernanceSeverity
{
    Info,
    Warning,
    Critical
}

public enum GovernanceCategory
{
    Certificate,
    Protocol,
    Security,
    Configuration
}

public sealed record GovernanceInsight
{
    public GovernanceSeverity Severity { get; init; }
    public GovernanceCategory Category { get; init; }
    public string Message { get; init; } = string.Empty;
}
