namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public sealed record ProtocolEvidence
{
    public string Source { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public string? Value { get; init; }
    public int Weight { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
}
