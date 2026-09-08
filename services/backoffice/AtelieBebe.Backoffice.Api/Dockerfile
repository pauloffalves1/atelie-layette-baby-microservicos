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
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Backoffice.Api.dll"]
