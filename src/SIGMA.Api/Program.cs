using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.OpenApi;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;
using SIGMA.Api.Endpoints;
using SIGMA.Api.Endpoints.Entra;
using SIGMA.Api.Hubs;
using SIGMA.Api.Middleware;
using SIGMA.Application;
using SIGMA.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;

// Prevent legacy XML claim mapping (so 'scp' remains 'scp', and 'oid' remains 'oid')
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog structured logging ──────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "SIGMA-API")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/sigma-api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

builder.Host.UseSerilog();

// ── OpenTelemetry distributed tracing ───────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(options =>
    {
        options.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddSource("SIGMA.API");
    });

var tenantId = builder.Configuration["AzureAd:TenantId"];
var apiClientId = builder.Configuration["AzureAd:ClientId"];
var sensitiveScope = builder.Configuration["Security:SensitiveScope"] ?? "sensitive_selfservice";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray()
    ?? ["http://localhost:3000", "http://localhost:3001", "https://localhost:3000"];

// Configure JWT Bearer authentication via Microsoft.Identity.Web
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var existingOnMessageReceived = options.Events.OnMessageReceived;
    options.Events.OnMessageReceived = async context =>
    {
        await existingOnMessageReceived(context);
        var accessToken = context.Request.Query["access_token"].FirstOrDefault();
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/sigma"))
        {
            context.Token = accessToken;
        }
    };
});

// DelegatedUserPolicy: requires a user-delegated token with access_as_user scope
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DelegatedUserPolicy", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var scp = context.User.FindFirst("scp")?.Value
                      ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;
            var oid = context.User.FindFirst("oid")?.Value
                      ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (scp == null || !scp.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("access_as_user") || oid == null)
            {
                return false;
            }

            return true;
        });
    });

    // SensitiveSelfServicePolicy: requires the sensitive_selfservice scope
    // for write/delete operations (e.g., updating app registrations, deleting users).
    options.AddPolicy("SensitiveSelfServicePolicy", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var scp = context.User.FindFirst("scp")?.Value
                      ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;
            var oid = context.User.FindFirst("oid")?.Value
                      ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (scp == null || oid == null) return false;

            var scopes = scp.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return scopes.Contains("access_as_user") && scopes.Contains(sensitiveScope);
        });
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Minimal APIs to serialize Enums as strings (e.g. "Critical" instead of 0)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("oid")?.Value
              ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
              ?? context.Connection.RemoteIpAddress?.ToString()
              ?? "authenticated"
            : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 120),
                Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:WindowMinutes", 1)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = builder.Configuration.GetValue("RateLimiting:QueueLimit", 20)
            });
    });
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<SignalREventBus>();
builder.Services.AddSingleton<SIGMA.Application.Abstractions.IEventBus>(sp => sp.GetRequiredService<SignalREventBus>());

// ── Health checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Output caching for read-heavy endpoints ─────────────────────────────────
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["OAuth2"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize"),
                    TokenUrl = new Uri($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { $"api://{apiClientId}/access_as_user", "Access SIGMA API on behalf of the signed-in user" }
                    }
                }
            },
            Description = "OAuth2 Authorization Code flow (Client App)"
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("OAuth2", document), new List<string>() }
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Enable forwarded headers (X-Forwarded-For, X-Forwarded-Proto)
// Required when deployed behind a load balancer (Azure App Service, AWS ALB, etc.)
// Configure via "ForwardedHeaders:Enabled" or env var "ForwardedHeaders__Enabled"
if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                           | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseResponseCompression();
app.UseOutputCache();

// HTTPS enforcement (skip in Development for local HTTP debugging)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.TryAdd("X-XSS-Protection", "0");
    context.Response.Headers.TryAdd("Cache-Control", "no-store");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SIGMA API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "SIGMA API — Swagger UI";
        options.OAuthClientId(builder.Configuration["SwaggerOAuth:ClientId"]);
        options.OAuthScopes($"api://{apiClientId}/access_as_user");
        options.OAuthUsePkce();
    });

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("SIGMA API")
               .WithTheme(ScalarTheme.Purple);
    });
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

var api = app.MapGroup("/api/v1");

api.MapHealthEndpoint();

var entra = api.MapGroup("/entra").RequireAuthorization("DelegatedUserPolicy");
entra.MapTenantEndpoints();
entra.MapUserEndpoints();
entra.MapGroupEndpoints();
entra.MapServicePrincipalEndpoints();
entra.MapApplicationEndpoints();

var governance = api.MapGroup("").RequireAuthorization("DelegatedUserPolicy");
governance.MapGovernanceEndpoints();

app.MapHub<SigmaHub>("/hubs/sigma")
    .RequireAuthorization("DelegatedUserPolicy");

app.Run();
