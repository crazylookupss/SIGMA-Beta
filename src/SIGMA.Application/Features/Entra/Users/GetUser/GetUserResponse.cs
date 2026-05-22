namespace SIGMA.Application.Features.Entra.Users.GetUser;

public sealed record GetUserResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? GivenName { get; init; }
    public string? Surname { get; init; }
    public string? JobTitle { get; init; }
    public string? Mail { get; init; }
    public string? MobilePhone { get; init; }
    public string? OfficeLocation { get; init; }
    public string? PreferredLanguage { get; init; }
    public string? BusinessPhone { get; init; }
    public bool? AccountEnabled { get; init; }
    public string? UserType { get; init; }
}
