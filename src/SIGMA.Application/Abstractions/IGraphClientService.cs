using SIGMA.Application.Common;
using SIGMA.Domain.Common;
using SIGMA.Domain.Entities;

namespace SIGMA.Application.Abstractions;

public interface IGraphClientService
{
    Task<Result<PagedResponse<EntraUser>>> GetUsersAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraUser>> GetUserByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<EntraGroup>>> GetGroupsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraGroup>> GetGroupByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<EntraServicePrincipal>>> GetServicePrincipalsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraServicePrincipal>> GetServicePrincipalByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<EntraApplication>>> GetApplicationsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraApplication>> GetApplicationByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);
}
