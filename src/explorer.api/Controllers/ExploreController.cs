namespace Explorer.Api.Controllers
{
    using System.Collections.Concurrent;
    using System.Net.Mime;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class ExploreController : ControllerBase
    {
        private static readonly ConcurrentDictionary<System.Guid, ColumnExplorer> Explorers
            = new ConcurrentDictionary<System.Guid, ColumnExplorer>();

        private readonly ILogger<ExploreController> logger;
        private readonly JsonApiClient apiClient;

        public ExploreController(ILogger<ExploreController> logger, JsonApiClient apiClient)
        {
            this.logger = logger;
            this.apiClient = apiClient;
        }

        [HttpPost]
        [Route("explore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Explore(Models.ExploreParams data)
        {
            var dataSources = await apiClient.GetDataSources();

            if (!dataSources.AsDict.TryGetValue(data.DataSourceName, out var exploreDataSource))
            {
                return BadRequest($"Could not find datasource '{data.DataSourceName}'.");
            }

            if (!exploreDataSource.TableDict.TryGetValue(data.TableName, out var exploreTableMeta))
            {
                return BadRequest($"Could not find table '{data.TableName}'.");
            }

            if (!exploreTableMeta.ColumnDict.TryGetValue(data.ColumnName, out var explorerColumnMeta))
            {
                return BadRequest($"Could not find column '{data.ColumnName}'.");
            }

            var explorerImpls = CreateExplorerImpls(explorerColumnMeta.Type, apiClient, data);
            if (explorerImpls == null)
            {
                return Ok(new Models.NotImplementedError
                {
                    Description = $"No exploration strategy implemented for {explorerColumnMeta.Type} columns.",
                    Data = data,
                });
            }

            var explorer = new ColumnExplorer();
            foreach (var impl in explorerImpls)
            {
                explorer.Spawn(impl);
            }

            if (!Explorers.TryAdd(explorer.ExplorationGuid, explorer))
            {
                throw new System.Exception("Failed to store explorer in Dict - This should never happen!");
            }

            return Ok(new ExploreResult(explorer.ExplorationGuid, ExploreResult.ExploreStatus.New));
        }

        [HttpGet]
        [Route("result/{exploreId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Result(System.Guid exploreId)
        {
            if (Explorers.TryGetValue(exploreId, out var explorer))
            {
                var exploreStatus = explorer.Completion().Status switch
                {
                    TaskStatus.RanToCompletion => ExploreResult.ExploreStatus.Complete,
                    TaskStatus.Running => ExploreResult.ExploreStatus.Processing,
                    _ => ExploreResult.ExploreStatus.Error,
                };

                return Ok(new ExploreResult(
                            explorer.ExplorationGuid,
                            exploreStatus,
                            explorer.ExploreMetrics));
            }
            else
            {
                return BadRequest($"Couldn't find explorer with id {exploreId}");
            }
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();

        private static ExplorerImpl[]? CreateExplorerImpls(AircloakType type, JsonApiClient apiClient, Models.ExploreParams data)
        {
            return type switch
            {
                AircloakType.Integer => new ExplorerImpl[] {
                    new IntegerColumnExplorer(apiClient, data),
                    new MinMaxExplorer(apiClient, data),
                },
                AircloakType.Real => new ExplorerImpl[] {
                    new RealColumnExplorer(apiClient, data),
                    new MinMaxExplorer(apiClient, data),
                },
                AircloakType.Text => new ExplorerImpl[] {
                    new TextColumnExplorer(apiClient, data),
                },
                AircloakType.Bool => new ExplorerImpl[] {
                    new BoolColumnExplorer(apiClient, data),
                },
                _ => null,
            };
        }
    }
}
