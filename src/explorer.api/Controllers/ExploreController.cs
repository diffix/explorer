namespace Explorer.Api.Controllers
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class ExploreController : ControllerBase
    {
        private static readonly ConcurrentDictionary<System.Guid, Exploration> Explorations
            = new ConcurrentDictionary<System.Guid, Exploration>();

        private readonly ILogger<ExploreController> logger;
        private readonly JsonApiClient apiClient;
        private readonly ExplorerApiAuthProvider authProvider;

        public ExploreController(
            ILogger<ExploreController> logger,
            JsonApiClient apiClient,
            IAircloakAuthenticationProvider authProvider)
        {
            this.logger = logger;
            this.apiClient = apiClient;
            this.authProvider = (ExplorerApiAuthProvider)authProvider;
        }

        [HttpPost]
        [Route("explore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Explore(Models.ExploreParams data)
        {
            authProvider.RegisterApiKey(data.ApiKey);

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

            var exploration = CreateExploration(explorerColumnMeta.Type, data);
            if (exploration == null)
            {
                return Ok(new Models.NotImplementedError
                {
                    Description = $"No exploration strategy implemented for {explorerColumnMeta.Type} columns.",
                    Data = data,
                });
            }

            if (!Explorations.TryAdd(exploration.ExplorationGuid, exploration))
            {
                throw new System.Exception("Failed to store explorer in Dict - This should never happen!");
            }

            return Ok(new ExploreResult(exploration.ExplorationGuid, ExploreResult.ExploreStatus.New));
        }

        [HttpGet]
        [Route("result/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Result(System.Guid explorationId)
        {
            if (Explorations.TryGetValue(explorationId, out var explorer))
            {
                var exploreStatus = explorer.Status switch
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

                var result = new ExploreResult(
                            explorer.ExplorationGuid,
                            exploreStatus,
                            metrics);

                if (exploreStatus == ExploreResult.ExploreStatus.Complete ||
                    exploreStatus == ExploreResult.ExploreStatus.Error)
                {
                    _ = Explorations.TryRemove(explorationId, out _);
                }

                return Ok(result);
            }
            else
            {
                return BadRequest($"Couldn't find explorer with id {explorationId}");
            }
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();

        private Exploration? CreateExploration(AircloakType type, Models.ExploreParams data)
        {
            var resolver = new AircloakQueryResolver(apiClient, data.DataSourceName);

            var components = type switch
            {
                AircloakType.Integer => new ExplorerBase[]
                {
                    new IntegerColumnExplorer(resolver, data.TableName, data.ColumnName),
                    new MinMaxExplorer(resolver, data.TableName, data.ColumnName),
                },
                AircloakType.Real => new ExplorerBase[]
                {
                    new RealColumnExplorer(resolver, data.TableName, data.ColumnName),
                    new MinMaxExplorer(resolver, data.TableName, data.ColumnName),
                },
                AircloakType.Text => new ExplorerBase[]
                {
                    new TextColumnExplorer(resolver, data.TableName, data.ColumnName),
                },
                AircloakType.Bool => new ExplorerBase[]
                {
                    new BoolColumnExplorer(resolver, data.TableName, data.ColumnName),
                },
                _ => System.Array.Empty<ExplorerBase>(),
            };

            if (components.Length == 0)
            {
                return null;
            }

            return new Exploration(components);
        }
    }
}
