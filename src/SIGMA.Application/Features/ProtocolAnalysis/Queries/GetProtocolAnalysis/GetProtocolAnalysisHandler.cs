using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Application.ProtocolAnalysis.Pipeline;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.ProtocolAnalysis.Queries.GetProtocolAnalysis;

internal sealed class GetProtocolAnalysisHandler(
    IProtocolAnalysisOrchestrator orchestrator)
    : IQueryHandler<GetProtocolAnalysisQuery, Result<ProtocolAnalysisResult>>
{
    public async Task<Result<ProtocolAnalysisResult>> Handle(
        GetProtocolAnalysisQuery query, CancellationToken ct)
    {
        try
        {
            var result = await orchestrator.AnalyzeAsync(query.ServicePrincipalId, ct);
            return Result.Success(result);
        }
        catch (InvalidOperationException ex)
        {
            return Error.NotFound("ProtocolAnalysis.NotFound", ex.Message);
        }
        catch (Exception ex)
        {
            return Error.ExternalService("ProtocolAnalysis.Error", $"Analysis failed: {ex.Message}");
        }
    }
}
