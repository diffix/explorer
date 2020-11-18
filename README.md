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


## Configuration

There are some configuration items that can be used to tune performance or adjust the query output. These can be set
using environment variables of the form `Explorer__{name}`. `__` is a double-underscore and `{name}` refers to one of the
following items:

#### **DefaultSamplesToPublish** _default = `20`_

  The number of rows of sample data to generate by default.
  This value can be overridden using the `SamplesToPublish` querystring parameter (see [Launching an exploration](#launching-an-exploration)).

#### **MaxConcurrentQueries** _default = `10`_

  The maximum number of concurrent queries that will be launched by Diffix Explorer `per data source`. Reducing this
  can reduce the load on Aircloak servers at the expense of slower analysis by the Explorer.

#### **PollingInterval** _default = `2000`_

  The polling interval in milliseconds when polling the Aircoak Api.

#### **MultiColumnEnabled** _default = `true`_

  Diffix Explorer can use multic-column aggregates to estimate the correlation between two columns for improved
  sample data output. This flag toggles said feature.

#### **MaxCorrelationDepth** _default = `2`_

  This controls the maximum size of the column groupings when querying for multi-column relationships. Ie. a value of
  `2` means we will consider relationships between two columns, `3` means three-column combinations are considered.
  Increasing this can significantly impact the load on Aircloak.

#### **TextColumnMinFactorForCategoricalSampling** _default = `0.05`_

  Decimal value betwen 0.0 and 1.0. Determines the threshold for categorical sampling of text columns. If the
  proportion of unsuppressed distinct values of a text column is lower than this threshold, text samples will be
  generated by combining substrings. Otherwise, we simply sample from known values.

## Usage

> You will need an access token for the Aircloak API. If you don't have one, ask your local Aircloak admin.

### Api Versions

All endpoints are versioned under an api schema root. The api root is `/api/vN`, where N is the
version number. At present, the api is at version 1 so the url root is `/api/v1`.

### Launching an exploration

Diffix Explorer exposes an `/explore` endpoint that expects a `POST` request containing the url of the Aircloak API,
and authentication token, and the dataset, table and column to analyse. Assuming you are running the Explorer on `localhost:5000` and you are targeting an Aircloak backend api at `https://attack.aircloak.com/api/`:

```bash
curl -k -X POST -H "Content-Type: application/json" http://localhost:5000/api/v1/explore \
  -d '{
   "ApiUrl":"https://attack.aircloak.com/api/",
   "ApiKey":"my_secret_key",
   "DataSource": "gda_banking",
   "Table":"loans",
   "Columns":["amount", "firstname"],
   "SamplesToPublish":25
   }'
```

This launches the column exploration and, if all goes well, returns a http 200 reponse with a json payload containing a unique `id`:

<details>
<summary><i>Expand/collapse example Json response.</i></summary>

<p>

```json
{
    "id": "e8b48ad3-846c-42da-89a9-d0cff9f86b10",
    "status": "New",
    "versionInfo": {
        "commitRef": "master",
        "commitHash": "1004ee1d86d4565889997d40ec4b05f90c83d078"
    },
    "dataSource": "gda_banking",
    "table": "loans",
    "columns": [
        {
            "column": "amount",
            "columnType": "unknown",
            "status": "New",
            "metrics": []
        },
        {
            "column": "duration",
            "columnType": "unknown",
            "status": "New",
            "metrics": []
        },
        {
            "column": "status",
            "columnType": "unknown",
            "status": "New",
            "metrics": []
        },
        {
            "column": "firstname",
            "columnType": "unknown",
            "status": "New",
            "metrics": []
        }
    ],
    "sampleData": [],
    "correlations": [],
    "errors": []
}
```

</p>
</details>

### Polling for results

You can use the exploration `id` to poll for results on the `/result` endpoint:
```bash
curl -k http://localhost:5000/api/v1/result/204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf
```

The body of the response should again contain a json payload with an indication of the processing status as well as any computed metrics, e.g.:

<details>
<summary><i>Expand/collapse example Json response.</i></summary>

<p>

```json
{
    "id": "e8b48ad3-846c-42da-89a9-d0cff9f86b10",
    "status": "Complete",
    "versionInfo": {
        "commitRef": "master",
        "commitHash": "1004ee1d86d4565889997d40ec4b05f90c83d078"
    },
    "dataSource": "gda_banking",
    "table": "loans",
    "columns": [
        {
            "column": "amount",
            "columnType": "integer",
            "status": "Complete",
            "metrics": [
                {
                    "name": "histogram.suppressed_ratio",
                    "value": 0.22535211267605634
                },
                {
                    "name": "histogram.value_counts",
                    "value": {
                        "totalCount": 781,
                        "suppressedCount": 176,
                        "nullCount": 0,
                        "totalRows": 98,
                        "suppressedRows": 1,
                        "nullRows": 0,
                        "nonSuppressedRows": 97,
                        "nonSuppressedCount": 605,
                        "nonSuppressedNonNullCount": 605,
                        "suppressedCountRatio": 0.22535211267605634,
                        "isCategorical": false
                    }
                },
                {
                    "name": "histogram.buckets",
                    "value": [
                        {
                            "bucketSize": 2000.0,
                            "lowerBound": 10000,
                            "count": 2,
                            "countNoise": 0
                        },
                        {
                            "bucketSize": 2000.0,
                            "lowerBound": 14000,
                            "count": 4,
                            "countNoise": 1.8
                        },
                        {
                            "bucketSize": 2000.0,
                            "lowerBound": 16000,
                            "count": 4,
                            "countNoise": 1.8
                        },
                        [...]
                    ]
                },
                {
                    "name": "distinct.is_categorical",
                    "value": false
                },
                {
                    "name": "histogram.suppressed_count",
                    "value": 176
                },
                {
                    "name": "quartile_estimates",
                    "value": [
                        58000,
                        90181.81818181818,
                        161000
                    ]
                },
                {
                    "name": "max",
                    "value": 550000.0
                },
                {
                    "name": "descriptive_stats",
                    "value": {
                        "entropy": -0.03264893260839615,
                        "mean": 113750.4132231405,
                        "mode": 89000,
                        "quartiles": [
                            58999.99999945224,
                            90999.99999925024,
                            161000.0000001889
                        ],
                        "standardDeviation": 77110.21732916344,
                        "variance": 5945985616.550817
                    }
                },
                {
                    "name": "sample_values",
                    "value": [
                        29000,
                        33000,
                        43000,
                        [...]
                    ]
                },
                {
                    "name": "distinct.values",
                    "value": [
                        {
                            "value": 140688,
                            "count": 4
                        },
                        {
                            "value": 192744,
                            "count": 4
                        },
                        [...]
                    ]
                },
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
                    "name": "distinct.value_count",
                    "value": 818
                },
                {
                    "name": "distinct.suppressed_count",
                    "value": 788
                },
                {
                    "name": "distribution_estimates",
                    "value": [
                        {
                            "name": "Gamma",
                            "distribution": "Γ(x; k = 2.0619445291126066, θ = 64214.336579162664)",
                            "goodness": [
                                {
                                    "method": "ChiSquare",
                                    "pValue": 8.368612181206357E-66,
                                    "significant": true,
                                    "rank": 0
                                },
                                {
                                    "method": "KolmogorovSmirnov",
                                    "pValue": 8.1579303259962E-41,
                                    "significant": true,
                                    "rank": 0
                                }
                            ]
                        },
                        [...]
                    ]
                },
                {
                    "name": "average_estimate",
                    "value": 113750.413223
                },
                {
                    "name": "min",
                    "value": 0
                },
                {
                    "name": "distinct.null_count",
                    "value": 0
                }
            ]
        },
        {
            "column": "duration",
            "columnType": "integer",
            "status": "Complete",
            "metrics": [
                {
                    "name": "distinct.is_categorical",
                    "value": true
                },
                {
                    "name": "max",
                    "value": 61.0
                },
                {
                    "name": "sample_values",
                    "value": [
                        24,
                        24,
                        60,
                        [...]
                    ]
                },
                {
                    "name": "distinct.values",
                    "value": [
                        {
                            "value": 60,
                            "count": 175
                        },
                        {
                            "value": 48,
                            "count": 167
                        },
                        [...]
                    ]
                },
                {
                    "name": "exploration_info",
                    "value": {
                        "dataSource": "gda_banking",
                        "table": "loans",
                        "column": "duration",
                        "columnType": "integer"
                    }
                },
                {
                    "name": "distinct.value_count",
                    "value": 828
                },
                {
                    "name": "distinct.suppressed_count",
                    "value": 0
                },
                {
                    "name": "min",
                    "value": 12
                },
                {
                    "name": "distinct.null_count",
                    "value": 0
                }
            ]
        },
        {
            "column": "status",
            "columnType": "text",
            "status": "Complete",
            "metrics": [
                {
                    "name": "distinct.is_categorical",
                    "value": true
                },
                {
                    "name": "sample_values",
                    "value": [
                        "C",
                        "A",
                        "A",
                        [...]
                    ]
                },
                {
                    "name": "distinct.values",
                    "value": [
                        {
                            "value": "C",
                            "count": 491
                        },
                        {
                            "value": "A",
                            "count": 258
                        },
                        {
                            "value": "D",
                            "count": 45
                        },
                        {
                            "value": "B",
                            "count": 30
                        }
                    ]
                },
                {
                    "name": "exploration_info",
                    "value": {
                        "dataSource": "gda_banking",
                        "table": "loans",
                        "column": "status",
                        "columnType": "text"
                    }
                },
                {
                    "name": "distinct.value_count",
                    "value": 824
                },
                {
                    "name": "distinct.suppressed_count",
                    "value": 0
                },
                {
                    "name": "text.length.values",
                    "value": [
                        {
                            "value": 1,
                            "count": 825
                        }
                    ]
                },
                {
                    "name": "text.length.counts",
                    "value": {
                        "totalCount": 825,
                        "suppressedCount": 0,
                        "nullCount": 0,
                        "totalRows": 1,
                        "suppressedRows": 0,
                        "nullRows": 0,
                        "nonSuppressedRows": 1,
                        "nonSuppressedCount": 825,
                        "nonSuppressedNonNullCount": 825,
                        "suppressedCountRatio": 0,
                        "isCategorical": true
                    }
                },
                {
                    "name": "distinct.null_count",
                    "value": 0
                },
                {
                    "name": "is_email",
                    "value": {
                        "isEmail": false
                    }
                }
            ]
        },
        {
            "column": "firstname",
            "columnType": "text",
            "status": "Complete",
            "metrics": [
                {
                    "name": "distinct.is_categorical",
                    "value": false
                },
                {
                    "name": "sample_values",
                    "value": [
                        "Brasti",
                        "Bristi",
                        "Chalyn",
                        [...]
                    ]
                },
                {
                    "name": "distinct.values",
                    "value": [
                        {
                            "value": "Dara",
                            "count": 5
                        },
                        {
                            "value": "Otto",
                            "count": 3
                        },
                        [...]
                    ]
                },
                {
                    "name": "exploration_info",
                    "value": {
                        "dataSource": "gda_banking",
                        "table": "loans",
                        "column": "firstname",
                        "columnType": "text"
                    }
                },
                {
                    "name": "distinct.value_count",
                    "value": 814
                },
                {
                    "name": "distinct.suppressed_count",
                    "value": 780
                },
                {
                    "name": "text.length.values",
                    "value": [
                        {
                            "value": 3,
                            "count": 24
                        },
                        {
                            "value": 4,
                            "count": 173
                        },
                        {
                            "value": 5,
                            "count": 397
                        },
                        [...]
                    ]
                },
                {
                    "name": "text.length.counts",
                    "value": {
                        "totalCount": 830,
                        "suppressedCount": 0,
                        "nullCount": 0,
                        "totalRows": 8,
                        "suppressedRows": 0,
                        "nullRows": 0,
                        "nonSuppressedRows": 8,
                        "nonSuppressedCount": 830,
                        "nonSuppressedNonNullCount": 830,
                        "suppressedCountRatio": 0,
                        "isCategorical": true
                    }
                },
                {
                    "name": "distinct.null_count",
                    "value": 0
                },
                {
                    "name": "is_email",
                    "value": {
                        "isEmail": false
                    }
                }
            ]
        }
    ],
    "sampleData": [
        [
            89408,
            24,
            "C",
            "Brasti"
        ],
        [
            162204,
            24,
            "C",
            "Bristi"
        ],
        [
            270754,
            12,
            "C",
            "Chalyn"
        ],
        [...]
    ],
    "correlations": [
        {
            "name": "correlations",
            "value": [
                {
                    "columns": [
                        "amount",
                        "status"
                    ],
                    "correlationFactor": 0.6973433065710264
                },
                [...]
            ]
        },
        {
            "name": "sampled_correlations",
            "value": [...]
        }
    ],
    "errors": []
}
```

</p>
</details>

### Cancellation

You can cancel an ongoing exploration using the (you guessed it) `/cancel` endpoint:

```bash
curl -k http://localhost:5000/api/v1/cancel/204f47b4-9c9d-46d2-bdb0-95ef3d61f8cf
```

### Limitations

Correlation analysis only works for 31 or fewer columns. This total does not include invariant columns, which are
pre-filtered by before correlation analysis. If the filtered list of columns contains more than 31 members, correlation
analysis is performed for the first 31 columns in the list only.

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

### Publishing the docker image

First, make sure you are a member of the Diffix org on [docker hub](https://hub.docker.com/orgs/diffix).

You can now create an access token for docker hub and use this to authenticate if you have not already done so. Details
[here](https://docs.docker.com/docker-hub/access-tokens/).

Now, from the project root, build the image with the desired tags. Note the image name needs to match the repo path on 
docker hub, which is `diffix/explorer`. For example:

```
docker build -t diffix/explorer:20.3 -t diffix/explorer:latest .
```

Finally, push the images to docker hub.
```
docker push diffix/explorer:20.3
docker push diffix/explorer:latest
```


## Additional reading

- [Project wiki](https://github.com/diffix/explorer/wiki)
- [Diffix-Birch research paper](https://arxiv.org/pdf/1806.02075.pdf)
