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
            ],
            ValidAudiences = [jwtAudience, "https://graph.microsoft.com"],
            SignatureValidator = (token, _) => new JsonWebToken(token),
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

app.Run();
