using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerUI;
using SIGMA.Api.Endpoints;
using SIGMA.Api.Endpoints.Auth;
using SIGMA.Api.Endpoints.Entra;
using SIGMA.Api.Middleware;
using SIGMA.Application;
using SIGMA.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var jwtAuthority = builder.Configuration["Authentication:Jwt:Authority"];
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"];
var tenantId = builder.Configuration["Entra:TenantId"];

var v1ConfigUrl = $"https://login.microsoftonline.com/{tenantId}/.well-known/openid-configuration";
var v2ConfigUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
var commonConfigUrl = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

var v1ConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    v1ConfigUrl,
    new OpenIdConnectConfigurationRetriever());

var v2ConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    v2ConfigUrl,
    new OpenIdConnectConfigurationRetriever());

var commonConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    commonConfigUrl,
    new OpenIdConnectConfigurationRetriever());

// Retrieve and merge signing keys from Tenant (v1 & v2) and Microsoft Common keychains at startup
var v1Config = await v1ConfigManager.GetConfigurationAsync();
var v2Config = await v2ConfigManager.GetConfigurationAsync();
var commonConfig = await commonConfigManager.GetConfigurationAsync();

var allSigningKeys = v1Config.SigningKeys
    .Concat(v2Config.SigningKeys)
    .Concat(commonConfig.SigningKeys)
    .GroupBy(k => k.KeyId)
    .Select(g => g.First())
    .ToList();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtAuthority;
        options.Audience = jwtAudience;
        options.TokenValidationParameters = new()
        {
            ValidIssuers =
            [
                jwtAuthority?.TrimEnd('/'),
                $"https://sts.windows.net/{tenantId}/",
                $"https://login.microsoftonline.com/{tenantId}",
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://login.microsoftonline.com/common/v2.0",
            ],
            ValidAudiences =
            [
                jwtAudience,
                "https://graph.microsoft.com",
                tenantId,
                builder.Configuration["Entra:ClientId"],
                $"api://{builder.Configuration["Entra:ClientId"]}"
            ],
            IssuerSigningKeys = allSigningKeys,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token authentication"
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SIGMA API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "SIGMA API — Swagger UI";
    });

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("SIGMA API")
               .WithTheme(ScalarTheme.Purple);
    });
}

app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api/v1");

api.MapHealthEndpoint();

var auth = api.MapGroup("/auth");
auth.MapTokenEndpoints();

var entra = api.MapGroup("/entra").RequireAuthorization();
entra.MapUserEndpoints();
entra.MapGroupEndpoints();
entra.MapServicePrincipalEndpoints();
entra.MapApplicationEndpoints();

app.Run();
