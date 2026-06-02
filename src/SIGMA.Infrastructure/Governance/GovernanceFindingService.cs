using Microsoft.Extensions.Logging;
using SIGMA.Application.Abstractions;
using SIGMA.Application.Caching;
using SIGMA.Application.Governance;
using SIGMA.Application.Governance.Models;

namespace SIGMA.Infrastructure.Governance;

internal sealed class GovernanceFindingService : IGovernanceFindingService
{
    private readonly IGraphClientService _graphClient;
    private readonly ICacheProvider _cache;
    private readonly ILogger<GovernanceFindingService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);
    private static readonly SemaphoreSlim Throttle = new(10, 10);

    private const int MaxEntitiesPerScan = 500;

    public GovernanceFindingService(
        IGraphClientService graphClient,
        ICacheProvider cache,
        ILogger<GovernanceFindingService> logger)
    {
        _graphClient = graphClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GovernanceFindingsResponse> GetFindingsAsync(bool forceRefresh, CancellationToken ct)
    {
        const string cacheKey = "governance_findings";
        if (!forceRefresh)
        {
            var cached = await _cache.GetAsync<GovernanceFindingsResponse>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogInformation("[Governance] Returning cached response with {Count} findings", cached.Findings.Count);
                return cached;
            }
        }

        var findings = new List<GovernanceFinding>();
        var now = DateTimeOffset.UtcNow;

        // ── 1. APPLICATIONS ──────────────────────────────────────────────────
        _logger.LogInformation("[Governance] Fetching applications from Graph...");
        var appsResult = await _graphClient.GetApplicationsAsync(
            null, null, MaxEntitiesPerScan, null, null, ct);

        _logger.LogInformation("[Governance] Applications result: IsSuccess={IsSuccess}, Error={Error}",
            appsResult.IsSuccess, appsResult.Error?.Description);

        if (appsResult.IsSuccess && appsResult.Value?.Data is { Count: > 0 } appList)
        {
            _logger.LogInformation("[Governance] Processing {Count} applications...", appList.Count);

            // Batch credential checks to avoid N+1 sequential HTTP calls
            const int credentialBatchSize = 20;
            for (var i = 0; i < appList.Count; i += credentialBatchSize)
            {
                var batch = appList.Skip(i).Take(credentialBatchSize).ToList();
                var credentialTasks = batch.Select(async app =>
                {
                    await Throttle.WaitAsync(ct);
                    try
                    {
                        var creds = await _graphClient.GetApplicationCredentialsAsync(app.Id, ct);
                        return (App: app, Creds: creds);
                    }
                    finally
                    {
                        Throttle.Release();
                    }
                });

                var results = await Task.WhenAll(credentialTasks);

                foreach (var result in results)
                {
                    var app = result.App;
                    var creds = result.Creds;

                    // Ownerless applications
                    if ((app.OwnersCount ?? 0) == 0)
                    {
                        findings.Add(CreateFinding(
                            $"app-no-owner-{app.Id}",
                            FindingCategory.Ownership, FindingSeverity.Critical,
                            "Application has no owners",
                            $"Application '{app.DisplayName}' has no owners assigned",
                            "Application", app.Id, app.DisplayName ?? "Unknown",
                            $"/app-registrations/{app.Id}"));
                    }

                    if (creds?.Secrets is null && creds?.Certificates is null) continue;

                    foreach (var secret in creds?.Secrets ?? [])
                    {
                        if (secret.EndDateTime is null) continue;

                        if (secret.IsExpired)
                        {
                            findings.Add(CreateFinding(
                                $"app-secret-expired-{app.Id}-{secret.KeyId}",
                                FindingCategory.Credentials, FindingSeverity.Critical,
                                "Application has expired client secret",
                                $"Secret '{secret.DisplayName ?? secret.KeyId}' expired on {secret.EndDateTime.Value:yyyy-MM-dd}",
                                "Application", app.Id, app.DisplayName ?? "Unknown",
                                $"/app-registrations/{app.Id}"));
                        }
                        else if (secret.IsExpiringSoon)
                        {
                            findings.Add(CreateFinding(
                                $"app-secret-expiring-{app.Id}-{secret.KeyId}",
                                FindingCategory.Credentials, FindingSeverity.Warning,
                                "Application has expiring client secret",
                                $"Secret '{secret.DisplayName ?? secret.KeyId}' expires on {secret.EndDateTime.Value:yyyy-MM-dd} ({secret.DaysUntilExpiry} days remaining)",
                                "Application", app.Id, app.DisplayName ?? "Unknown",
                                $"/app-registrations/{app.Id}"));
                        }
                    }

                    foreach (var cert in creds?.Certificates ?? [])
                    {
                        if (cert.EndDateTime is null) continue;

                        if (cert.IsExpired)
                        {
                            findings.Add(CreateFinding(
                                $"app-cert-expired-{app.Id}-{cert.KeyId}",
                                FindingCategory.Credentials, FindingSeverity.Critical,
                                "Application has expired certificate",
                                $"Certificate '{cert.DisplayName ?? cert.KeyId}' expired on {cert.EndDateTime.Value:yyyy-MM-dd}",
                                "Application", app.Id, app.DisplayName ?? "Unknown",
                                $"/app-registrations/{app.Id}"));
                        }
                        else if (cert.IsExpiringSoon)
                        {
                            findings.Add(CreateFinding(
                                $"app-cert-expiring-{app.Id}-{cert.KeyId}",
                                FindingCategory.Credentials, FindingSeverity.Warning,
                                "Application has expiring certificate",
                                $"Certificate '{cert.DisplayName ?? cert.KeyId}' expires on {cert.EndDateTime.Value:yyyy-MM-dd} ({cert.DaysUntilExpiry} days remaining)",
                                "Application", app.Id, app.DisplayName ?? "Unknown",
                                $"/app-registrations/{app.Id}"));
                        }
                    }
                }
            }
        }
        else
        {
            _logger.LogWarning("[Governance] Apps fetch failed or returned no data. IsSuccess={IsSuccess} Count={Count}",
                appsResult.IsSuccess, appsResult.Value?.Data?.Count ?? -1);

            if (!appsResult.IsSuccess)
            {
                findings.Add(CreateFinding(
                    "error-apps", FindingCategory.Configuration, FindingSeverity.Warning,
                    "Graph API: Unable to analyze applications",
                    $"Failed to fetch applications: {appsResult.Error?.Description ?? "Check Azure AD permissions (Application.Read.All)"}",
                    "System", "error", "Graph API Error", "/"));
            }
        }

        // ── 2. GROUPS ─────────────────────────────────────────────────────────
        _logger.LogInformation("[Governance] Fetching groups from Graph...");
        var groupsResult = await _graphClient.GetGroupsAsync(null, null, MaxEntitiesPerScan, null, null, ct);

        _logger.LogInformation("[Governance] Groups result: IsSuccess={IsSuccess}, Count={Count}",
            groupsResult.IsSuccess, groupsResult.Value?.Data?.Count ?? -1);

        if (groupsResult.IsSuccess && groupsResult.Value?.Data is { Count: > 0 } groupList)
        {
            foreach (var group in groupList)
            {
                _logger.LogDebug("[Governance] Group: {Name}, OwnersCount={Owners}, Members={Members}",
                    group.DisplayName, group.OwnersCount, group.TotalDirectMembers);

                // OwnersCount nullable — treat null as no owner info (flag it)
                var hasNoOwner = (group.OwnersCount ?? 0) == 0;
                var hasMembers = (group.TotalDirectMembers ?? group.MemberCount ?? 0) > 0;

                if (hasNoOwner && hasMembers)
                {
                    _logger.LogInformation("[Governance] Found ownerless group: {Name}", group.DisplayName);
                    findings.Add(CreateFinding(
                        $"group-no-owner-{group.Id}",
                        FindingCategory.Ownership, FindingSeverity.Critical,
                        "Group has no owners",
                        $"Group '{group.DisplayName}' has members but no assigned owner",
                        "Group", group.Id, group.DisplayName ?? "Unknown",
                        $"/groups/{group.Id}"));
                }
            }
        }
        else if (!groupsResult.IsSuccess)
        {
            findings.Add(CreateFinding(
                "error-groups", FindingCategory.Configuration, FindingSeverity.Warning,
                "Graph API: Unable to analyze groups",
                $"Failed to fetch groups: {groupsResult.Error?.Description ?? "Check Azure AD permissions (Group.Read.All)"}",
                "System", "error", "Graph API Error", "/"));
        }

        // ── 3. USERS ──────────────────────────────────────────────────────────
        _logger.LogInformation("[Governance] Fetching users from Graph...");
        var usersResult = await _graphClient.GetUsersAsync(
            "id,displayName,accountEnabled,lastPasswordChangeDateTime,mobilePhone,userType",
            null, MaxEntitiesPerScan, null, null, ct);

        _logger.LogInformation("[Governance] Users result: IsSuccess={IsSuccess}, Count={Count}",
            usersResult.IsSuccess, usersResult.Value?.Data?.Count ?? -1);

        if (usersResult.IsSuccess && usersResult.Value?.Data is { Count: > 0 } userList)
        {
            foreach (var user in userList)
            {
                // Disabled accounts
                if (user.AccountEnabled == false)
                {
                    findings.Add(CreateFinding(
                        $"user-disabled-{user.Id}",
                        FindingCategory.Security, FindingSeverity.Info,
                        "User account is disabled",
                        $"User '{user.DisplayName}' account is currently disabled",
                        "User", user.Id, user.DisplayName ?? "Unknown",
                        $"/users/{user.Id}"));
                }

                // Stale passwords (>90 days)
                if (user.LastPasswordChangeDateTime.HasValue &&
                    (now - user.LastPasswordChangeDateTime.Value).TotalDays > 90)
                {
                    findings.Add(CreateFinding(
                        $"user-stale-password-{user.Id}",
                        FindingCategory.Security, FindingSeverity.Warning,
                        "User has stale password",
                        $"Password last changed {user.LastPasswordChangeDateTime.Value:yyyy-MM-dd} ({(int)(now - user.LastPasswordChangeDateTime.Value).TotalDays} days ago)",
                        "User", user.Id, user.DisplayName ?? "Unknown",
                        $"/users/{user.Id}"));
                }

                // MFA check: no mobile phone registered (Member users only)
                if (string.IsNullOrEmpty(user.MobilePhone) && user.UserType == "Member")
                {
                    findings.Add(CreateFinding(
                        $"user-no-mfa-{user.Id}",
                        FindingCategory.Security, FindingSeverity.Critical,
                        "User may not have MFA registered",
                        $"User '{user.DisplayName}' has no mobile phone / MFA recovery method on record",
                        "User", user.Id, user.DisplayName ?? "Unknown",
                        $"/users/{user.Id}"));
                }
            }
        }
        else if (!usersResult.IsSuccess)
        {
            findings.Add(CreateFinding(
                "error-users", FindingCategory.Configuration, FindingSeverity.Warning,
                "Graph API: Unable to analyze users",
                $"Failed to fetch users: {usersResult.Error?.Description ?? "Check Azure AD permissions (User.Read.All)"}",
                "System", "error", "Graph API Error", "/"));
        }

        // ── Deduplicate, sort, build response ─────────────────────────────────
        findings = findings.GroupBy(f => f.Id).Select(g => g.First()).ToList();
        findings = [.. findings.OrderBy(f => f.Severity).ThenBy(f => f.EntityName)];

        _logger.LogInformation("[Governance] Final findings count: {Total} (Critical={Critical}, Warning={Warning}, Info={Info})",
            findings.Count,
            findings.Count(f => f.Severity == FindingSeverity.Critical),
            findings.Count(f => f.Severity == FindingSeverity.Warning),
            findings.Count(f => f.Severity == FindingSeverity.Info));

        var response = new GovernanceFindingsResponse
        {
            Summary = new GovernanceSummary
            {
                Total = findings.Count,
                Critical = findings.Count(f => f.Severity == FindingSeverity.Critical),
                Warning = findings.Count(f => f.Severity == FindingSeverity.Warning),
                Info = findings.Count(f => f.Severity == FindingSeverity.Info),
                LastAnalyzed = DateTimeOffset.UtcNow,
            },
            Findings = findings,
        };

        await _cache.SetAsync(cacheKey, response, CacheTtl, ct);
        return response;
    }

    // ──────────────────────────────────────────────────────────────────────────
    private static GovernanceFinding CreateFinding(
        string id, FindingCategory category, FindingSeverity severity,
        string title, string description, string entityType, string entityId,
        string entityName, string actionUrl) => new()
        {
            Id = id,
            Category = category,
            Severity = severity,
            Title = title,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            ActionUrl = actionUrl,
        };

    public async Task<GovernanceFindingsResponse> GetFindingsByCategoryAsync(FindingCategory category, CancellationToken ct)
    {
        var all = await GetFindingsAsync(false, ct);
        var filtered = all.Findings.Where(f => f.Category == category).ToList();
        return new GovernanceFindingsResponse
        {
            Summary = new GovernanceSummary
            {
                Total = filtered.Count,
                Critical = filtered.Count(f => f.Severity == FindingSeverity.Critical),
                Warning = filtered.Count(f => f.Severity == FindingSeverity.Warning),
                Info = filtered.Count(f => f.Severity == FindingSeverity.Info),
                LastAnalyzed = all.Summary.LastAnalyzed,
            },
            Findings = filtered,
        };
    }

    public async Task<GovernanceFindingsResponse> GetFindingsBySeverityAsync(FindingSeverity severity, CancellationToken ct)
    {
        var all = await GetFindingsAsync(false, ct);
        var filtered = all.Findings.Where(f => f.Severity == severity).ToList();
        return new GovernanceFindingsResponse
        {
            Summary = new GovernanceSummary
            {
                Total = filtered.Count,
                Critical = filtered.Count(f => f.Severity == FindingSeverity.Critical),
                Warning = filtered.Count(f => f.Severity == FindingSeverity.Warning),
                Info = filtered.Count(f => f.Severity == FindingSeverity.Info),
                LastAnalyzed = all.Summary.LastAnalyzed,
            },
            Findings = filtered,
        };
    }
}
