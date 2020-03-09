namespace Explorer.Api.Controllers
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading;
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
            this.RegisterApiKey(data.ApiKey);

            using var cts = new CancellationTokenSource();
            var dataSources = await apiClient.GetDataSources(cts.Token);

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

            var exploration = CreateExploration(explorerColumnMeta.Type, data, cts);
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
                throw new System.Exception("Failed to store exploration in Dict - This should never happen!");
            }

            return Ok(new ExploreResult(exploration.ExplorationGuid, ExploreResult.ExploreStatus.New));
        }

        [HttpGet]
        [Route("result/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Result(System.Guid explorationId)
        {
            if (Explorations.TryGetValue(explorationId, out var exploration))
            {
                var exploreStatus = exploration.Status;

                var metrics = exploration.ExploreMetrics
                    .Select(m => new ExploreResult.Metric(m.Name, m.Metric));

                var result = new ExploreResult(
                            exploration.ExplorationGuid,
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
                return BadRequest($"Couldn't find exploration with id {explorationId}");
            }
        }

        [HttpGet]
        [Route("cancel/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Cancel(System.Guid explorationId)
        {
            if (!Explorations.TryGetValue(explorationId, out var exploration))
            {
                return BadRequest($"Couldn't find exploration with id {explorationId}");
            }
            exploration.Cancel();
            return Ok();
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();

        private Exploration? CreateExploration(AircloakType type, Models.ExploreParams data, CancellationTokenSource cts)
        {
            var resolver = new AircloakQueryResolver(apiClient, data.DataSourceName);

            var components = type switch
            {
                AircloakType.Integer => new ExplorerBase[]
                {
                    new IntegerColumnExplorer(resolver, data.TableName, data.ColumnName, cts.Token),
                    new MinMaxExplorer(resolver, data.TableName, data.ColumnName, cts.Token),
                },
                AircloakType.Real => new ExplorerBase[]
                {
                    new RealColumnExplorer(resolver, data.TableName, data.ColumnName, cts.Token),
                    new MinMaxExplorer(resolver, data.TableName, data.ColumnName, cts.Token),
                },
                AircloakType.Text => new ExplorerBase[]
                {
                    new TextColumnExplorer(resolver, data.TableName, data.ColumnName, cts.Token),
                },
                AircloakType.Bool => new ExplorerBase[]
                {
                    new BoolColumnExplorer(resolver, data.TableName, data.ColumnName, cts.Token),
                },
                _ => System.Array.Empty<ExplorerBase>(),
            };

            if (components.Length == 0)
            {
                return null;
            }

            return new Exploration(components, cts);
        }
    }
}
