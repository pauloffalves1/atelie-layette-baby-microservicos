FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/AtelieBebe.SharedKernel/AtelieBebe.SharedKernel.csproj shared/AtelieBebe.SharedKernel/
COPY services/orders/AtelieBebe.Orders.Core/AtelieBebe.Orders.Core.csproj services/orders/AtelieBebe.Orders.Core/
COPY services/orders/AtelieBebe.Orders.Api/AtelieBebe.Orders.Api.csproj services/orders/AtelieBebe.Orders.Api/
RUN dotnet restore services/orders/AtelieBebe.Orders.Api/AtelieBebe.Orders.Api.csproj

COPY shared/AtelieBebe.SharedKernel/ shared/AtelieBebe.SharedKernel/
COPY services/orders/ services/orders/
RUN dotnet publish services/orders/AtelieBebe.Orders.Api/AtelieBebe.Orders.Api.csproj -c Release -o /app --no-restore

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
ENTRYPOINT ["dotnet", "AtelieBebe.Orders.Api.dll"]
