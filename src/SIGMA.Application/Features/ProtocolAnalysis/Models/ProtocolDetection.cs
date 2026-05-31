namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public sealed record ProtocolDetection
{
    public AuthenticationProtocol Protocol { get; init; }
    public ProtocolConfidence Confidence { get; init; }
    public int Score { get; init; }
    public int MaxPossibleScore { get; init; }
    public double NormalizedScore => MaxPossibleScore > 0 ? Math.Round((double)Score / MaxPossibleScore * 100, 1) : 0;
    public List<ProtocolEvidence> Evidence { get; init; } = [];
    public bool IsDetected => Score > 0;
}
