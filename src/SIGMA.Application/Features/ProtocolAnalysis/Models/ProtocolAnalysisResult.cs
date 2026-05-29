namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public sealed record ProtocolAnalysisResult
{
    public AuthenticationProtocol PrimaryProtocol { get; init; }
    public List<ProtocolDetection> DetectedProtocols { get; init; } = [];
    public List<ProtocolEvidence> AllEvidence { get; init; } = [];
    public List<GovernanceInsight> GovernanceInsights { get; init; } = [];
    public DateTimeOffset AnalysisTimestamp { get; init; }
    public string? ServicePrincipalId { get; init; }
    public string? ServicePrincipalDisplayName { get; init; }
}
