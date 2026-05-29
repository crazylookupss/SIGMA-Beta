using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Abstractions;

public interface IProtocolDetector
{
    AuthenticationProtocol Protocol { get; }
    Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default);
}
