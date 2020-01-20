# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

# copy csproj and restore as distinct layers
WORKDIR /src/explorer.api/
COPY explorer.api/*.csproj .
RUN dotnet restore

# copy everything else
COPY . /src
# build explorer.api
WORKDIR /src/explorer.api/
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "explorer.api.dll"]
