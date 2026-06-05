# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore in a separate layer for Docker cache efficiency
COPY ["src/PropFlow.Api/PropFlow.Api.csproj",                       "PropFlow.Api/"]
COPY ["src/PropFlow.Application/PropFlow.Application.csproj",       "PropFlow.Application/"]
COPY ["src/PropFlow.Domain/PropFlow.Domain.csproj",                 "PropFlow.Domain/"]
COPY ["src/PropFlow.Infrastructure/PropFlow.Infrastructure.csproj", "PropFlow.Infrastructure/"]
RUN dotnet restore "PropFlow.Api/PropFlow.Api.csproj"

COPY src/ .
RUN dotnet publish "PropFlow.Api/PropFlow.Api.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Non-root user for container security
RUN addgroup --system propflow \
    && adduser --system --ingroup propflow propflow
USER propflow

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PropFlow.Api.dll"]
