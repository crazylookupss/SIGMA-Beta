namespace SIGMA.Application.Governance.Models;

public enum FindingSeverity
{
    Critical,
    Warning,
    Info
}

public enum FindingCategory
{
    Credentials,
    Ownership,
    Protocol,
    Configuration,
    Security,
    Compliance
}

public sealed record GovernanceFinding
{
    public string Id { get; init; } = string.Empty;
    public FindingCategory Category { get; init; }
    public FindingSeverity Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string ActionUrl { get; init; } = string.Empty;
    public Dictionary<string, string> Metadata { get; init; } = [];
}

public sealed record GovernanceSummary
{
    public int Total { get; init; }
    public int Critical { get; init; }
    public int Warning { get; init; }
    public int Info { get; init; }
    public DateTimeOffset LastAnalyzed { get; init; }
}

public sealed record GovernanceFindingsResponse
{
    public GovernanceSummary Summary { get; init; } = new();
    public List<GovernanceFinding> Findings { get; init; } = [];
}
