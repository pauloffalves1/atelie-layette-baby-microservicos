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
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AtelieBebe.Notifications.Worker.dll"]
