FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/AtelieBebe.SharedKernel/AtelieBebe.SharedKernel.csproj shared/AtelieBebe.SharedKernel/
COPY services/catalog/AtelieBebe.Catalog.Core/AtelieBebe.Catalog.Core.csproj services/catalog/AtelieBebe.Catalog.Core/
COPY services/catalog/AtelieBebe.Catalog.Api/AtelieBebe.Catalog.Api.csproj services/catalog/AtelieBebe.Catalog.Api/
RUN dotnet restore services/catalog/AtelieBebe.Catalog.Api/AtelieBebe.Catalog.Api.csproj

COPY shared/AtelieBebe.SharedKernel/ shared/AtelieBebe.SharedKernel/
COPY services/catalog/ services/catalog/
RUN dotnet publish services/catalog/AtelieBebe.Catalog.Api/AtelieBebe.Catalog.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Catalog.Api.dll"]
