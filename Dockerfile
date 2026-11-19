FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend/UniversityDecisionHub.Api/UniversityDecisionHub.Api.csproj Backend/UniversityDecisionHub.Api/
RUN dotnet restore Backend/UniversityDecisionHub.Api/UniversityDecisionHub.Api.csproj
COPY Backend/ Backend/
COPY Frontend/ Frontend/
RUN dotnet publish Backend/UniversityDecisionHub.Api/UniversityDecisionHub.Api.csproj -c Release -o /out /p:UseAppHost=false
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.12
WORKDIR /src/Backend/UniversityDecisionHub.Api
# Design-time only: the bundle command constructs the context without opening this DB.
RUN ConnectionStrings__DefaultConnection='Host=localhost;Database=UniversityDecisionHub;Username=build;Password=build' \
    DOTNET_ENVIRONMENT=Development \
    Jwt__Key='BuildTimeOnlyKeyNotUsedByTheRunningDemo123456' \
    /tools/dotnet-ef migrations bundle --configuration Release --context ApplicationDbContext --output /out/migrate

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS demo
WORKDIR /app/Backend/UniversityDecisionHub.Api
COPY --from=build /out/ ./
COPY Frontend/ /app/Frontend/
COPY DataBase/Imports/catalog.ready.json /app/catalog/catalog.ready.json
RUN chmod +x ./migrate
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "UniversityDecisionHub.Api.dll"]
