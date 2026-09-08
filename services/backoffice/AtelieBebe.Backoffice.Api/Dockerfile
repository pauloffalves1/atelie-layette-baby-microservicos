FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/AtelieBebe.SharedKernel/AtelieBebe.SharedKernel.csproj shared/AtelieBebe.SharedKernel/
COPY services/backoffice/AtelieBebe.Backoffice.Core/AtelieBebe.Backoffice.Core.csproj services/backoffice/AtelieBebe.Backoffice.Core/
COPY services/backoffice/AtelieBebe.Backoffice.Api/AtelieBebe.Backoffice.Api.csproj services/backoffice/AtelieBebe.Backoffice.Api/
RUN dotnet restore services/backoffice/AtelieBebe.Backoffice.Api/AtelieBebe.Backoffice.Api.csproj

COPY shared/AtelieBebe.SharedKernel/ shared/AtelieBebe.SharedKernel/
COPY services/backoffice/ services/backoffice/
RUN dotnet publish services/backoffice/AtelieBebe.Backoffice.Api/AtelieBebe.Backoffice.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# New Relic .NET agent — license key and app name are supplied at runtime via docker-compose.yml
# (NEW_RELIC_LICENSE_KEY comes from .env, never baked in here).
RUN apt-get update && apt-get install -y wget ca-certificates \
    && wget -q https://download.newrelic.com/dot_net_agent/latest_release/newrelic-dotnet-agent_amd64.tar.gz -O /tmp/newrelic-agent.tar.gz \
    && tar -xzf /tmp/newrelic-agent.tar.gz -C /usr/local \
    && rm /tmp/newrelic-agent.tar.gz \
    && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so

COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Backoffice.Api.dll"]
