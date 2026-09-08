FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY services/gateway/AtelieBebe.Gateway/AtelieBebe.Gateway.csproj services/gateway/AtelieBebe.Gateway/
RUN dotnet restore services/gateway/AtelieBebe.Gateway/AtelieBebe.Gateway.csproj

COPY services/gateway/ services/gateway/
RUN dotnet publish services/gateway/AtelieBebe.Gateway/AtelieBebe.Gateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# New Relic .NET agent — license key and app name are supplied at runtime via docker-compose.yml
# (NEW_RELIC_LICENSE_KEY comes from .env, never baked in here).
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
    && wget -q -O- https://download.newrelic.com/548C16BF.gpg | gpg --dearmor -o /usr/share/keyrings/newrelic-archive-keyring.gpg \
    && echo 'deb [signed-by=/usr/share/keyrings/newrelic-archive-keyring.gpg] http://apt.newrelic.com/debian/ newrelic non-free' > /etc/apt/sources.list.d/newrelic.list \
    && apt-get update \
    && apt-get install -y newrelic-dotnet-agent \
    && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so

COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Gateway.dll"]
