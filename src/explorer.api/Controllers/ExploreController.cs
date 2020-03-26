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
        private readonly ExplorerApiAuthProvider authProvider;
        private readonly ExplorerConfig config;

        public ExploreController(
            ILogger<ExploreController> logger,
            JsonApiClient apiClient,
            IAircloakAuthenticationProvider authProvider,
            ExplorerConfig config)
        {
            this.config = config;
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

            var dataSources = await apiClient.GetDataSources(CancellationToken.None);

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

            var resolver = new AircloakQueryResolver(apiClient, data.DataSourceName, config.PollFrequencyTimeSpan);

#pragma warning disable CA2000 // call IDisposable.Dispose
            var exploration = Exploration.Create(resolver, explorerColumnMeta.Type, data.TableName, data.ColumnName);
#pragma warning restore CA2000 // call IDisposable.Dispose
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
        public async Task<IActionResult> Result(System.Guid explorationId)
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

                if (exploration.Completion.IsCompleted)
                {
                    try
                    {
                        // await the completion task to trigger any inner exceptions
                        await exploration.Completion;
                    }
                    finally
                    {
                        Explorations.TryRemove(explorationId, out _);
                        exploration.Dispose();
                    }
                }

                return Ok(result);
            }
            else
            {
                return NotFound($"Couldn't find explorer with id {explorationId}");
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
            return Ok(true);
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();
    }
}
