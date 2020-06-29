# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

ENV BUILD_TARGET src/explorer.api/explorer.api.csproj
ENV BUILD_LOG /var/log/explorer-build.log

# copy the csproj files and restore as distinct layers
# https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#leverage-build-cache
COPY src/diffix/*.csproj /src/diffix/	
COPY src/aircloak/*.csproj /src/aircloak/	
COPY src/explorer/*.csproj /src/explorer/	
COPY src/explorer.api/*.csproj /src/explorer.api/
RUN dotnet restore $BUILD_TARGET

# copy everything else
COPY . . 
# publish the project
RUN dotnet publish $BUILD_TARGET -c release -o /app --no-restore > $BUILD_LOG

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

ARG COMMIT_HASH="Unknown"
ARG COMMIT_REF="N/A"

ENV Explorer__CommitHash=$COMMIT_HASH
ENV Explorer__CommitRef=$COMMIT_REF

WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "explorer.api.dll"]
