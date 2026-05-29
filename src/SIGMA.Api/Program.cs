using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;
using SIGMA.Api.Endpoints;
using SIGMA.Api.Endpoints.Entra;
using SIGMA.Api.Hubs;
using SIGMA.Api.Middleware;
using SIGMA.Application;
using SIGMA.Infrastructure;
using System.IdentityModel.Tokens.Jwt;

// Prevent legacy XML claim mapping (so 'scp' remains 'scp', and 'oid' remains 'oid')
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

var tenantId = builder.Configuration["AzureAd:TenantId"];
var apiClientId = builder.Configuration["AzureAd:ClientId"];
var sensitiveScope = builder.Configuration["Security:SensitiveScope"] ?? "sensitive_selfservice";

// Configure JWT Bearer authentication via Microsoft.Identity.Web
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

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
            var appid = context.User.FindFirst("appid")?.Value;
            var iss = context.User.FindFirst("iss")?.Value;

            if (scp == null || !scp.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("access_as_user") || oid == null)
            {
                Console.WriteLine($"[DelegatedUserPolicy] DENIED — scp=[{scp}] oid=[{oid}] appid=[{appid}] iss=[{iss}]");
                return false;
            }

            Console.WriteLine($"[DelegatedUserPolicy] ALLOWED — scp=[{scp}] oid=[{oid}]");
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
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<SignalREventBus>();
builder.Services.AddSingleton<SIGMA.Application.Abstractions.IEventBus>(sp => sp.GetRequiredService<SignalREventBus>());

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
app.UseAuthorization();

var api = app.MapGroup("/api/v1");

api.MapHealthEndpoint();

var entra = api.MapGroup("/entra").RequireAuthorization("DelegatedUserPolicy");
entra.MapTenantEndpoints();
entra.MapUserEndpoints();
entra.MapGroupEndpoints();
entra.MapServicePrincipalEndpoints();
entra.MapApplicationEndpoints();

app.MapHub<SigmaHub>("/hubs/sigma");

app.Run();
