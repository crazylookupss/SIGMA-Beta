using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetDevices;

internal sealed class GetGroupDevicesHandler(IGraphClientService graphClient)
    : IQueryHandler<GetGroupDevicesQuery, Result<PagedResponse<GroupDeviceResponse>>>
{
    public async Task<Result<PagedResponse<GroupDeviceResponse>>> Handle(
        GetGroupDevicesQuery query, CancellationToken cancellationToken)
    {
        var devices = await graphClient.GetGroupDevicesAsync(query.Id, cancellationToken);
        return Result.Success(new PagedResponse<GroupDeviceResponse>
        {
            Data = devices.Select(d => new GroupDeviceResponse
            {
                Id = d.Id,
                DisplayName = d.DisplayName,
                DeviceId = d.DeviceId,
                OperatingSystem = d.OperatingSystem,
                OsVersion = d.OsVersion,
                IsCompliant = d.IsCompliant,
                IsManaged = d.IsManaged,
                TrustType = d.TrustType,
            }).ToList(),
            Count = devices.Count,
        });
    }
}
