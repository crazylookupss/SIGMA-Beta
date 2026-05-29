using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetAuditLogs;

public sealed record GetGroupAuditLogsQuery(string Id, int? Top = 50) : IQuery<Result<PagedResponse<GroupAuditLogResponse>>>;
