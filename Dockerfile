# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

ENV SLN_FILE explorer.deploy.sln

# copy the sln and csproj files and restore as distinct layers
# https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#leverage-build-cache
COPY $SLN_FILE /
COPY src/diffix/*.csproj /src/diffix/
COPY src/aircloak/*.csproj /src/aircloak/
COPY src/explorer/*.csproj /src/explorer/
COPY src/explorer.api/*.csproj /src/explorer.api/
RUN dotnet restore $SLN_FILE

# copy everything else
COPY . . 
# build solution 
RUN dotnet publish $SLN_FILE -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "explorer.api.dll"]
