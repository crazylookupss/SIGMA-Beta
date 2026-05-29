namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public sealed record ProtocolDetection
{
    public AuthenticationProtocol Protocol { get; init; }
    public ProtocolConfidence Confidence { get; init; }
    public int Score { get; init; }
    public List<ProtocolEvidence> Evidence { get; init; } = [];
    public bool IsDetected => Score > 0;
}
