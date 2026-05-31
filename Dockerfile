FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/SIGMA.Api/SIGMA.Api.csproj", "src/SIGMA.Api/"]
COPY ["src/SIGMA.Application/SIGMA.Application.csproj", "src/SIGMA.Application/"]
COPY ["src/SIGMA.Domain/SIGMA.Domain.csproj", "src/SIGMA.Domain/"]
COPY ["src/SIGMA.Infrastructure/SIGMA.Infrastructure.csproj", "src/SIGMA.Infrastructure/"]
RUN dotnet restore "src/SIGMA.Api/SIGMA.Api.csproj"
COPY . .
WORKDIR "/src/src/SIGMA.Api"
RUN dotnet build "SIGMA.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SIGMA.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SIGMA.Api.dll"]
