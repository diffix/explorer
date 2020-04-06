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

Building and running can be simply done using Docker. The http api is exposed on port 5000. 

You will need specify two environment variables: 
- `AIRCLOAK_API_KEY`: The api key mentioned above.
- `AIRCLOAK_API_URL`: The base url for the api, for example `https://attack.aircloak.com`

It is recommended to define `AIRCLOAK_API_KEY` in the host environment and to specify the `AIRCLOAK_API_URL` explicitly on startup. For example:

```
docker build -t explorer .
docker run -it --rm  -e AIRCLOAK_API_KEY -e AIRCLOAK_API_URL:"https://attack.airclaoak.com" -p 5000:80 explorer
```

If you are running in a unix-like environment, you use or adapt the `build.sh` and `run.sh` scripts:

```
./build.sh
./run.sh "https://attack.aircloak.com/api"
```


## Development

The simplest way to get started is with VS Code's remote containers feature.

> Detailed information on setting this up can be found 
[here](https://code.visualstudio.com/docs/remote/containers#_getting-started).

The short version:

1. Install [Docker](https://www.docker.com/get-started)
2. Install [Visual Studio Code](https://code.visualstudio.com/)
3. Add the [remote development pack](https://aka.ms/vscode-remote/download/extension) in VS Code
4. Clone the Repo: `git clone https://github.com/diffix/explorer.git`
5. Start VS Code and from the command palette (`F1`) run _Remote-Containers: Open Folder in Container_ and select the project root folder.

If you want to use an editor other than VS Code, you will need [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) to compile the source files on your local machine.

## Additional reading

- [Project wiki](https://github.com/diffix/explorer/wiki)
- [Diffix-Birch research paper](https://arxiv.org/pdf/1806.02075.pdf)
