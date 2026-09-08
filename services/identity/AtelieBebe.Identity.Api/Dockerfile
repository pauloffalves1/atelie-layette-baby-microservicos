# Build context is the microservices/ root (not this folder) — see docker-compose.yml — so this
# Dockerfile can reach both its own service folder and the shared kernel project.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/AtelieBebe.SharedKernel/AtelieBebe.SharedKernel.csproj shared/AtelieBebe.SharedKernel/
COPY services/identity/AtelieBebe.Identity.Core/AtelieBebe.Identity.Core.csproj services/identity/AtelieBebe.Identity.Core/
COPY services/identity/AtelieBebe.Identity.Api/AtelieBebe.Identity.Api.csproj services/identity/AtelieBebe.Identity.Api/
RUN dotnet restore services/identity/AtelieBebe.Identity.Api/AtelieBebe.Identity.Api.csproj

COPY shared/AtelieBebe.SharedKernel/ shared/AtelieBebe.SharedKernel/
COPY services/identity/ services/identity/
RUN dotnet publish services/identity/AtelieBebe.Identity.Api/AtelieBebe.Identity.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Identity.Api.dll"]
