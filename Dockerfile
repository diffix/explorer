ARG BUILD_LOG=/var/log/explorer-build.log

# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

ARG BUILD_LOG
ARG BUILD_TARGET=src/explorer.api/explorer.api.csproj

# copy the csproj files and restore as distinct layers
# https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#leverage-build-cache
WORKDIR /build
COPY src/diffix/*.csproj src/diffix/
COPY src/aircloak/*.csproj src/aircloak/
COPY src/explorer/*.csproj src/explorer/
COPY src/explorer.api/*.csproj src/explorer.api/
RUN dotnet restore $BUILD_TARGET

# copy everything else
COPY . .
# publish the project
RUN dotnet publish $BUILD_TARGET -c release -o /app --no-restore > $BUILD_LOG
# add a list of banned words used for generating sample text values from common substrings
ADD http://www.bannedwordlist.com/lists/swearWords.txt /app/bannedwords.txt

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

ARG BUILD_LOG

WORKDIR /app
COPY --from=build /app ./
COPY --from=build $BUILD_LOG $BUILD_LOG
ENTRYPOINT ["dotnet", "explorer.api.dll"]
