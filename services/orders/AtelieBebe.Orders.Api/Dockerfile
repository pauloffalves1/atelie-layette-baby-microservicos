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
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Orders.Api.dll"]
