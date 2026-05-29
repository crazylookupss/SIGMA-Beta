namespace SIGMA.Application.Features.Entra.Groups.GetAuditLogs;

public sealed record GroupAuditLogResponse
{
    public string Id { get; init; } = string.Empty;
    public string? ActivityDisplayName { get; init; }
    public string? Category { get; init; }
    public string? InitiatedBy { get; init; }
    public string? TargetResourceName { get; init; }
    public string? Result { get; init; }
    public string? ResultReason { get; init; }
    public DateTimeOffset? ActivityDateTime { get; init; }
    public string? CorrelationId { get; init; }
}
