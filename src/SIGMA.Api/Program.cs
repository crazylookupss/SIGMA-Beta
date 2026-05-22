using Scalar.AspNetCore;
using SIGMA.Api.Authentication;
using SIGMA.Api.Endpoints;
using SIGMA.Api.Endpoints.Entra;
using SIGMA.Api.Middleware;
using SIGMA.Application;
using SIGMA.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddJwtBearer()
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationSchemeOptions.DefaultScheme, null);

builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

var entra = api.MapGroup("/entra");
entra.MapUserEndpoints();
entra.MapGroupEndpoints();
entra.MapServicePrincipalEndpoints();

app.Run();
