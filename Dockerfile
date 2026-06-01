FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SIGMA.Api/SIGMA.Api.csproj src/SIGMA.Api/
COPY src/SIGMA.Application/SIGMA.Application.csproj src/SIGMA.Application/
COPY src/SIGMA.Infrastructure/SIGMA.Infrastructure.csproj src/SIGMA.Infrastructure/
RUN dotnet restore src/SIGMA.Api/SIGMA.Api.csproj

COPY src/ src/
RUN dotnet publish src/SIGMA.Api/SIGMA.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5107

ENV ASPNETCORE_URLS=http://+:5107
ENV DOTNET_ENVIRONMENT=Development

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SIGMA.Api.dll"]
