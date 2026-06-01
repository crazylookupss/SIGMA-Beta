using FluentValidation;
using SIGMA.Application.Features.Entra.Applications.ListApplications;
using SIGMA.Application.Features.Entra.Groups.ListGroups;
using SIGMA.Application.Features.Entra.Groups.GetAuditLogs;
using SIGMA.Application.Features.Entra.ServicePrincipals.ListServicePrincipals;
using SIGMA.Application.Features.Entra.Users.ListUsers;
using SIGMA.Application.Features.Entra.Applications.GetApplication;
using SIGMA.Application.Features.Entra.Groups.GetGroup;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipal;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalProxyConfig;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalSsoConfig;
using SIGMA.Application.Features.Entra.Users.GetUser;
using SIGMA.Application.Features.Entra.Groups.GetAccessReviews;
using SIGMA.Application.Features.Entra.Groups.GetDevices;
using SIGMA.Application.Features.Entra.Groups.GetApplications;
using SIGMA.Application.Features.Entra.Groups.GetOwners;
using SIGMA.Application.Features.Entra.Groups.GetMembers;
using SIGMA.Application.Features.ProtocolAnalysis.Queries.GetProtocolAnalysis;

namespace SIGMA.Application.Common;

// ── List Query Validators ──────────────────────────────────────────────────
// Shared paging validation rules for all list queries

internal sealed class ListApplicationsQueryValidator : AbstractValidator<ListApplicationsQuery>
{
    public ListApplicationsQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 999).When(x => x.Top.HasValue);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);
    }
}

internal sealed class ListGroupsQueryValidator : AbstractValidator<ListGroupsQuery>
{
    public ListGroupsQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 999).When(x => x.Top.HasValue);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);
    }
}

internal sealed class ListServicePrincipalsQueryValidator : AbstractValidator<ListServicePrincipalsQuery>
{
    public ListServicePrincipalsQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 999).When(x => x.Top.HasValue);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);
    }
}

internal sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 999).When(x => x.Top.HasValue);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);
    }
}

// ── Entity ID Validators ───────────────────────────────────────────────────
// Validate that entity IDs are non-empty and look like GUIDs

internal sealed class GetApplicationQueryValidator : AbstractValidator<GetApplicationQuery>
{
    public GetApplicationQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Application ID is required.");
    }
}

internal sealed class GetGroupQueryValidator : AbstractValidator<GetGroupQuery>
{
    public GetGroupQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Group ID is required.");
    }
}

internal sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("User ID is required.");
    }
}

internal sealed class GetServicePrincipalQueryValidator : AbstractValidator<GetServicePrincipalQuery>
{
    public GetServicePrincipalQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Service principal ID is required.");
    }
}

internal sealed class GetServicePrincipalProxyConfigQueryValidator : AbstractValidator<GetServicePrincipalProxyConfigQuery>
{
    public GetServicePrincipalProxyConfigQueryValidator()
    {
        RuleFor(x => x.ServicePrincipalId).NotEmpty().WithMessage("Service principal ID is required.");
    }
}

internal sealed class GetServicePrincipalSsoConfigQueryValidator : AbstractValidator<GetServicePrincipalSsoConfigQuery>
{
    public GetServicePrincipalSsoConfigQueryValidator()
    {
        RuleFor(x => x.ServicePrincipalId).NotEmpty().WithMessage("Service principal ID is required.");
    }
}

internal sealed class GetGroupAuditLogsQueryValidator : AbstractValidator<GetGroupAuditLogsQuery>
{
    public GetGroupAuditLogsQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Group ID is required.");
        RuleFor(x => x.Top).InclusiveBetween(1, 500).When(x => x.Top.HasValue);
    }
}

internal sealed class GetGroupAccessReviewsQueryValidator : AbstractValidator<GetGroupAccessReviewsQuery>
{
    public GetGroupAccessReviewsQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Group ID is required.");
    }
}

internal sealed class GetGroupDevicesQueryValidator : AbstractValidator<GetGroupDevicesQuery>
{
    public GetGroupDevicesQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Group ID is required.");
    }
}

internal sealed class GetGroupApplicationsQueryValidator : AbstractValidator<GetGroupApplicationsQuery>
{
    public GetGroupApplicationsQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Group ID is required.");
    }
}

internal sealed class GetGroupOwnersQueryValidator : AbstractValidator<GetGroupOwnersQuery>
{
    public GetGroupOwnersQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Group ID is required.");
    }
}

internal sealed class GetGroupMembersQueryValidator : AbstractValidator<GetGroupMembersQuery>
{
    public GetGroupMembersQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Group ID is required.");
    }
}

internal sealed class GetProtocolAnalysisQueryValidator : AbstractValidator<GetProtocolAnalysisQuery>
{
    public GetProtocolAnalysisQueryValidator()
    {
        RuleFor(x => x.ServicePrincipalId).NotEmpty().WithMessage("Service principal ID is required.");
    }
}
