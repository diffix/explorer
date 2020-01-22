# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

# copy the sln and csproj files and restore as distinct layers
# https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#leverage-build-cache
COPY explorer.sln /
COPY src/explorer.api/*.csproj /src/explorer.api/
COPY src/aircloak/*.csproj /src/aircloak/
RUN dotnet restore 

# copy everything else
COPY . . 
# build solution 
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "explorer.api.dll"]
