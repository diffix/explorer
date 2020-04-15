# Explorer

- [What it does](#what-it-does)
- [Getting started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running](#running)
  - [Development](#development)
- [Additional reading](#additional-reading)

----------------------

## What it does

Anonymized data from the Diffix-protected datasets is inherently restricted. The analyst needs to be familiar with the imposed limitations, and knowledgeable of possible workarounds. The aim of this project is to build tools to automatically extract a high-level picture of the shape of a given data set whilst intelligently navigating the restrictions imposed by Diffix.

## Getting started

### Prerequisites

#### Aircloak API Key

You will need an authorization key for the Aircloak API. This should be assigned to the `AIRCLOAK_API_KEY`
variable in your environment. 

### Running

The simplest way to get started is using Docker. The http api is exposed on port 5000. 

You will need to assign the Aircloak Api endpoint to the `AIRCLOAK_API_URL` environment variable, for example the following exposes the api on port `5000` using the Aircloak Api at `https://attack.aircloak.com/api/`:

```
docker build -t explorer .
docker run -it --rm -e AIRCLOAK_API_URL="https://attack.aircloak.com/api/" -p 5000:80 explorer
```

If you are running in a unix-like environment, you can use or adapt the `build.sh` and `run.sh` scripts. The following is equivalent to the above command.

```
./build.sh
./run.sh https://attack.aircloak.com/api/ 5000
```

> Note you will also need an access token for the Aircloak Api. This token is passed with the request to the `/explore` endpoint.

## Usage

### Launching an exploration

The explorer exposes an `/explore` endpoint that expects a `POST` request containing the dataset, table and column to analyse. Assuming you are running the explorer on `localhost:5000`: 
```bash
curl -k -X POST -H "Content-Type: application/json" http://localhost:5000/explore \
  -d "{
   \"ApiKey\":\"my_secret_key\", 
   \"DataSourceName\": \"gda_banking\", 
   \"TableName\":\"loans\",
   \"ColumnName\":\"amount\"
   }"
```

This launches the column exploration and, if all goes well, returns a successful reponse with a json payload containing a unique id: 
```json
{
  "status":"New",
  "metrics":[],
  "id":"204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf"
}
```

### Polling for results

You can use the id to poll for results on the `/results` endpoint:
```bash
curl -k http://localhost:5000/result/204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf
```

The body of the response should again contain a json payload with an indication of the processing status as well as any computed metrics. For example for a text column: 

```json
{
  "status":"Processing",
  "id":"204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf",
  "metrics":[
    {"name": "text.length.distinct.suppressed_count", "value": 16},
    {"name": "text.length.distinct.values",
      "value": [
        {"value": 24, "count": 256},
        {"value": 25, "count": 254},
        {"value": 27, "count": 242},
        {"..."},
        {"value": 51, "count": 6},
        {"value": 9, "count": 4},
        {"value": 8, "count": 2}]},
    {"name": "text.length.naive_max", "value": 49},
    {"name": "text.length.naive_min", "value": 15}
  ]
}
```

When exploration is complete, this is indicated with `"status": "Complete"`.

### Cancellation

You can cancel an ongoing exploration using the (you guessed it) `/cancel` endpoint:

```bash
curl -k http://localhost:5000/cancel/204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf
```

### More examples

For further examples, check out the basic [client implementations](src/clients).


## Development

The simplest way to get a development environment up and running is with VS Code's remote containers feature. 

> Detailed information on setting this up can be found 
[here](https://code.visualstudio.com/docs/remote/containers#_getting-started).

The short version:

1. Install [Docker](https://www.docker.com/get-started)
2. Install [Visual Studio Code](https://code.visualstudio.com/)
3. Add the [remote development pack](https://aka.ms/vscode-remote/download/extension) in VS Code
4. Clone the Repo: `git clone https://github.com/diffix/explorer.git`
5. Start VS Code and from the command palette (`F1`) run _Remote-Containers: Open Folder in Container_ and select the project root folder.

If you want to use an editor other than VS Code, you will need [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) to compile the source files on your local machine.

## Testing

Running the tests requires the `AIRCLOAK_API_KEY` environment variable to be set to a valid api key. If you are using vs code remote containers, this environment variable will be propagated from your local environment to the development container. 

## Additional reading

- [Project wiki](https://github.com/diffix/explorer/wiki)
- [Diffix-Birch research paper](https://arxiv.org/pdf/1806.02075.pdf)
