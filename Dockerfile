# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

# copy the csproj files and restore as distinct layers
# https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#leverage-build-cache
COPY src/diffix/*.csproj /src/diffix/	
COPY src/aircloak/*.csproj /src/aircloak/	
COPY src/explorer/*.csproj /src/explorer/	
COPY src/explorer.api/*.csproj /src/explorer.api/
RUN dotnet restore src/explorer.api/explorer.api.csproj

# copy everything else
COPY . . 
# publish the project
RUN dotnet publish src/explorer.api/explorer.api.csproj -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "explorer.api.dll"]
