# Web-based clients configuration

In order to use the web-based clients `map.html` and `client.html`, it is required to create a file `client_config.js` in this folder, and define the following constants with the values filled according to the environment setup and desired behaviour.

```js
const EXPLORER_BASE_URL = 'http://localhost:5000/api/v1';
const AIRCLOAK_API_KEY = 'api key string'
const AIRCLOAK_API_URL = 'https://demo.aircloak.com/api/';
```

Probably in most of the cases the clients will be loaded directly from the local file system, and this requires that the Explorer instance is launched in development mode, so that `CORS` is enabled, e.g.:

```dotnetcli
dotnet run --environment Development --project .\src\explorer.api\explorer.api.csproj
```

Or if using docker, add `-e "ASPNETCORE_ENVIRONMENT=Development"` to your `run` command, e.g.:
```
docker run -e "ASPNETCORE_ENVIRONMENT=Development" -it --rm -p 5000:80 explorer
```
