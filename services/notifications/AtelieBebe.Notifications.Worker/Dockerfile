FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/AtelieBebe.SharedKernel/AtelieBebe.SharedKernel.csproj shared/AtelieBebe.SharedKernel/
COPY services/notifications/AtelieBebe.Notifications.Worker/AtelieBebe.Notifications.Worker.csproj services/notifications/AtelieBebe.Notifications.Worker/
RUN dotnet restore services/notifications/AtelieBebe.Notifications.Worker/AtelieBebe.Notifications.Worker.csproj

COPY shared/AtelieBebe.SharedKernel/ shared/AtelieBebe.SharedKernel/
COPY services/notifications/ services/notifications/
RUN dotnet publish services/notifications/AtelieBebe.Notifications.Worker/AtelieBebe.Notifications.Worker.csproj -c Release -o /app --no-restore

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
ENTRYPOINT ["dotnet", "AtelieBebe.Notifications.Worker.dll"]
