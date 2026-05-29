using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetAuditLogs;

internal sealed class GetGroupAuditLogsHandler(IGraphClientService graphClient)
    : IQueryHandler<GetGroupAuditLogsQuery, Result<PagedResponse<GroupAuditLogResponse>>>
{
    public async Task<Result<PagedResponse<GroupAuditLogResponse>>> Handle(
        GetGroupAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var logs = await graphClient.GetGroupAuditLogsAsync(query.Id, query.Top ?? 50, cancellationToken);
        return Result.Success(new PagedResponse<GroupAuditLogResponse>
        {
            Data = logs.Select(l => new GroupAuditLogResponse
            {
                Id = l.Id,
                ActivityDisplayName = l.ActivityDisplayName,
                Category = l.Category,
                InitiatedBy = l.InitiatedBy,
                TargetResourceName = l.TargetResourceName,
                Result = l.Result,
                ResultReason = l.ResultReason,
                ActivityDateTime = l.ActivityDateTime,
                CorrelationId = l.CorrelationId,
            }).ToList(),
            Count = logs.Count,
        });
    }
}
