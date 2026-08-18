FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY HelpDesk.DatabaseMigrator/HelpDesk.DatabaseMigrator.csproj HelpDesk.DatabaseMigrator/
RUN dotnet restore HelpDesk.DatabaseMigrator/HelpDesk.DatabaseMigrator.csproj

COPY Db/HelpDeskDbContext.cs Db/
COPY Models/ Models/
COPY Migrations/ Migrations/
COPY HelpDesk.DatabaseMigrator/ HelpDesk.DatabaseMigrator/

RUN dotnet publish HelpDesk.DatabaseMigrator/HelpDesk.DatabaseMigrator.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final

WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "HelpDesk.DatabaseMigrator.dll"]
