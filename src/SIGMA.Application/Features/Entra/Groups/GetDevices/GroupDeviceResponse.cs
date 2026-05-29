namespace SIGMA.Application.Features.Entra.Groups.GetDevices;

public sealed record GroupDeviceResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? DeviceId { get; init; }
    public string? OperatingSystem { get; init; }
    public string? OsVersion { get; init; }
    public bool? IsCompliant { get; init; }
    public bool? IsManaged { get; init; }
    public string? TrustType { get; init; }
}
