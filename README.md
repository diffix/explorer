[![Build Status](https://travis-ci.com/diffix/explorer.svg?branch=master)](https://travis-ci.com/diffix/explorer)

# Diffix Explorer

- [What it does](#what-it-does)
- [Getting started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running](#running)
  - [Usage](#usage)
  - [Development](#development)
- [Additional reading](#additional-reading)

----------------------

## What it does

Anonymized data from the Diffix-protected datasets is inherently restricted. The analyst needs to be familiar with the imposed limitations, and knowledgeable of possible workarounds. The aim of this project is to build tools to automatically extract a high-level picture of the shape of a given data set whilst intelligently navigating the restrictions imposed by Diffix.

## Getting started

### Prerequisites

#### Aircloak API Key

You will need an authorization token for the Aircloak API. This should be assigned to the `AIRCLOAK_API_KEY`
variable in your environment.

#### Docker

Not a strict requirement, but the easiest way to get started is with [Docker](https://www.docker.com/get-started).

The most up-to-date version of the API is published as a docker image in the github registry, tagged `latest`.

In order to pull from the github registry you need to authenticate with a github access token:
1. Go [here](https://github.com/settings/tokens) and create a new token with the `read:packages` permission.
2. Save it in a file, for example `github_registry_token.txt`
3. Authenticate with docker login using the generated token for your github username:
    ```
    cat github_registry_token.txt | docker login docker.pkg.github.com -u $GITHUB_USERNAME --password-stdin
    ```
See [here](https://help.github.com/en/packages/using-github-packages-with-your-projects-ecosystem/configuring-docker-for-use-with-github-packages) for further information.

## Running

### Pre-built Docker Image

With the above out of the way, you can download and run the latest image with a single docker command.

For example, the following starts the Explorer in the foreground and exposes the API on port `5000`:

```
docker run -it --rm \
    -p 5000:80 \
    docker.pkg.github.com/diffix/explorer/explorer-api:latest
```

The following command is better suited for running the Explorer long term. It will give the container a name
and restart the service should it crash or the host be restarted.

```
docker run \
    -d \
    --name explorer \
    --restart unless-stopped \
    -p 5000:80 \
    docker.pkg.github.com/diffix/explorer/explorer-api:latest
```

### Docker build

You can also build and run the docker image locally.

```
# 1. Clone this repo
git clone https://github.com/diffix/explorer.git

# 2. Build the docker image
docker build -t explorer explorer

# 3. Run the application in a new container
docker run -it --rm -p 5000:80 explorer
```


## Usage

> You will need an access token for the Aircloak API. If you don't have one, ask your local Aircloak admin.

### Launching an exploration

Diffix Explorer exposes an `/explore` endpoint that expects a `POST` request containing the url of the Aircloak API,
and authentication token, and the dataset, table and column to analyse. Assuming you are running the Explorer on `localhost:5000` and you are targeting `https://attack.aircloak.com/api/`:

```bash
curl -k -X POST -H "Content-Type: application/json" http://localhost:5000/explore \
  -d "{
   \"ApiUrl\":\"https://attack.aircloak.com/api/\"
   \"ApiKey\":\"my_secret_key\",
   \"DataSource\": \"gda_banking\",
   \"Table\":\"loans\",
   \"Columns\":[\"amount\", \"firstname\"]
   }"
```

This launches the column exploration and, if all goes well, returns a http 200 reponse with a json payload containing a unique `id`:
```json
{
    "id": "e55a1a4a-a0e4-4673-9ecf-fae66a704d7d",
    "status": "New",
    "versionInfo": {
        "commitRef": "master",
        "commitHash": "7a35d2c8cd661947a6916179b49e6381f4878268"
    },
    "dataSource": "gda_banking",
    "table": "loans",
    "columns": [],
    "sampleData": [],
    "errors": []
}
```


### Polling for results

You can use the exploration `id` to poll for results on the `/result` endpoint:
```bash
curl -k http://localhost:5000/result/204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf
```

The body of the response should again contain a json payload with an indication of the processing status as well as any computed metrics, e.g. for integer and text columns:

```json
{
  "versionInfo": {
    "commitHash": "7a35d2c8cd661947a6916179b49e6381f4878268",
    "commitRef": "master"
  },
  "id":"204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf",
  "status":"Processing",
  "dataSource": "gda_banking",
  "table":"loans",
  "columns":[
    {
      "column":"amount",
      "metrics":[
        {
          "name": "exploration_info",
          "value": {
            "dataSource": "gda_banking",
            "table": "loans",
            "column": "amount",
            "columnType": "integer"
          }
        },
        {
          "name": "distinct.is_categorical",
          "value": false
        },
        {
          "name": "refined_min",
          "value": 3303
        },
        {
          "name": "refined_max",
          "value": 495103
        },
        {
          "name": "average_estimate",
          "value": 113750.413223
        },
        {
          "name": "quartile_estimates",
          "value": [ 58000, 90181.81818181818, 161000 ]
        },
        {
          "name": "sample_values",
          "value": [ 11000, 37000, 47000, 57000, 61000, 95000, 95000, 101000, 117000, 137000, 141000, 159000, 171000, 185000, 203000, 271000, 285000, 309000, 309000, 369000 ]
        }
      ],
      "status":"Processing"
    },
    {
      "column": "firstname",
      "metrics": [
        {
          "name": "exploration_info",
          "value": {
            "dataSource": "gda_banking",
            "table": "loans",
            "column": "firstname"
          }
        },
        {
          "name": "is_email",
          "value": false
        },
        {
          "name": "distinct.is_categorical",
          "value": false
        },
        {
          "name": "sample_values",
          "value": [ "Stenor", "Brad", "Ele", "Abrley", "Chritia", "Brieine", "Wynley", "Cam", "Jusson", "Sam" ]
        }
      ],
      "status":"Processing"
    }
  ],
  "sampleData": [
    [ 17000, "Clatau" ],
    [ 39000, "Bryina" ],
    [ 45000, "Ale" ],
    [ 67000, "Zen" ],
    [ 73000, "Jorlle" ],
    [ 91000, "Cole" ],
    [ 103000, "Jessti" ],
    [ 117000, "Abra" ],
    [ 123000, "Pric" ],
    [ 137000, "Karlie" ],
    [ 149000, "Just" ],
    [ 259000, "Quison" ],
    [ 309000, "Madlle" ],
    [ null, "Abra" ],
    [ null, "Darson" ],
  ],
  "errors": []
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

> Detailed information on setting up remote containers for VS Code can be found
[here](https://code.visualstudio.com/docs/remote/containers#_getting-started).

The short version:

1. Install [Docker](https://www.docker.com/get-started)
2. Install [Visual Studio Code](https://code.visualstudio.com/)
3. Add the [remote development pack](https://aka.ms/vscode-remote/download/extension) in VS Code
4. Start VS Code and from the command palette (`F1`) select _Remote-Containers: Open Repository in Container_.
5. Enter the url for this project: `https://github.com/diffix/explorer.git`
6. Let VS Code do its magic.

If you want to use an editor other than VS Code, you will need [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) to compile the source files on your local machine.

### Testing

Many of the tests run against data sources hosted at `https://attack.aircloak.com/api/`. To run the tests you will need to set the `AIRCLOAK_API_KEY` environment variable to a token that is valid for accessing this Aircloak instance. If you are using vs code remote containers, this environment variable will be propagated from your local environment to the development container.

## Additional reading

- [Project wiki](https://github.com/diffix/explorer/wiki)
- [Diffix-Birch research paper](https://arxiv.org/pdf/1806.02075.pdf)
