using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetDevices;

public sealed record GetGroupDevicesQuery(string Id) : IQuery<Result<PagedResponse<GroupDeviceResponse>>>;
