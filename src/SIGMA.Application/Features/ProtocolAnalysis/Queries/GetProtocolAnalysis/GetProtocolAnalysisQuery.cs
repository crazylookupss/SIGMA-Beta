using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.ProtocolAnalysis.Queries.GetProtocolAnalysis;

public sealed record GetProtocolAnalysisQuery(
    string ServicePrincipalId
) : IQuery<Result<ProtocolAnalysisResult>>;
