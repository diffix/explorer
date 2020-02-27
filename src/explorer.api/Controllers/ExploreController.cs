namespace Explorer.Api.Controllers
{
    using System.Collections.Concurrent;
    using System.Linq;
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

            var explorer = CreateColumnExplorer(explorerColumnMeta.Type, data);
            if (explorer == null)
            {
                return Ok(new Models.NotImplementedError
                {
                    Description = $"No exploration strategy implemented for {explorerColumnMeta.Type} columns.",
                    Data = data,
                });
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
                    TaskStatus.Canceled => ExploreResult.ExploreStatus.Complete,
                    TaskStatus.Created => ExploreResult.ExploreStatus.New,
                    TaskStatus.Faulted => ExploreResult.ExploreStatus.Error,
                    TaskStatus.RanToCompletion => ExploreResult.ExploreStatus.Complete,
                    TaskStatus.Running => ExploreResult.ExploreStatus.Processing,
                    TaskStatus.WaitingForActivation => ExploreResult.ExploreStatus.New,
                    TaskStatus.WaitingToRun => ExploreResult.ExploreStatus.New,
                    TaskStatus.WaitingForChildrenToComplete => ExploreResult.ExploreStatus.Processing,
                    var status => throw new System.Exception("Unexpected TaskStatus: '{status}'."),
                };

                var metrics = explorer.ExploreMetrics
                    .Select(m => new ExploreResult.Metric(m.Name, m.Metric));

                return Ok(new ExploreResult(
                            explorer.ExplorationGuid,
                            exploreStatus,
                            metrics));
            }
            else
            {
                return BadRequest($"Couldn't find explorer with id {exploreId}");
            }
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();

        private ColumnExplorer? CreateColumnExplorer(AircloakType type, Models.ExploreParams data)
        {
            var resolver = new AircloakQueryResolver(apiClient, data.DataSourceName);

            var components = type switch
            {
                AircloakType.Integer => new ExplorerImpl[]
                {
                    new IntegerColumnExplorer(resolver, data.TableName, data.ColumnName),
                    new MinMaxExplorer(resolver, data.TableName, data.ColumnName),
                },
                AircloakType.Real => new ExplorerImpl[]
                {
                    new RealColumnExplorer(resolver, data.TableName, data.ColumnName),
                    new MinMaxExplorer(resolver, data.TableName, data.ColumnName),
                },
                AircloakType.Text => new ExplorerImpl[]
                {
                    new TextColumnExplorer(resolver, data.TableName, data.ColumnName),
                },
                AircloakType.Bool => new ExplorerImpl[]
                {
                    new BoolColumnExplorer(resolver, data.TableName, data.ColumnName),
                },
                _ => System.Array.Empty<ExplorerImpl>(),
            };

            if (components.Length == 0)
            {
                return null;
            }

            var explorer = new ColumnExplorer();
            foreach (var component in components)
            {
                explorer.Spawn(component);
            }

            return explorer;
        }
    }
}
