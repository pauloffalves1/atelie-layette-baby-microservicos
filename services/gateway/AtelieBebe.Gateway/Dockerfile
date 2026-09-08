FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY services/gateway/AtelieBebe.Gateway/AtelieBebe.Gateway.csproj services/gateway/AtelieBebe.Gateway/
RUN dotnet restore services/gateway/AtelieBebe.Gateway/AtelieBebe.Gateway.csproj

COPY services/gateway/ services/gateway/
RUN dotnet publish services/gateway/AtelieBebe.Gateway/AtelieBebe.Gateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Gateway.dll"]
