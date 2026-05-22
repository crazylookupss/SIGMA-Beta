namespace SIGMA.Application.Common;

public sealed record PagedResponse<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public string? NextLink { get; init; }
    public int? Count { get; init; }
}
