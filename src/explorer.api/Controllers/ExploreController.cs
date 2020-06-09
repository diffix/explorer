namespace Explorer.Api.Controllers
{
    using System.Linq;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer;
    using Explorer.Api.Authentication;
    using Explorer.Api.Models;
    using Explorer.Metrics;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class ExploreController : ControllerBase
    {
        private readonly ILogger<ExploreController> logger;
        private readonly ExplorationRegistry explorationRegistry;

        public ExploreController(
            ILogger<ExploreController> logger,
            ExplorationRegistry explorationRegistry)
        {
            this.logger = logger;
            this.explorationRegistry = explorationRegistry;
        }

        [HttpPost]
        [Route("explore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Explore(
            ExploreParams data,
            [FromServices] ExplorationLauncher launcher,
            [FromServices] IAircloakAuthenticationProvider authProvider,
            [FromServices] ContextBuilder contextBuilder,
            [FromServices] AircloakConnectionBuilder connectionBuilder)
        {
            // Register the authentication token for this scope.
            if (authProvider is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(data.ApiKey);
            }

            // Create the Context and Connection objects for this exploration.
            var cts = new CancellationTokenSource();
            var ctx = await contextBuilder.Build(data);
            var conn = connectionBuilder.Build(data, cts.Token);

            // Get the configuration based on column type.
            var config = ComponentComposition.ColumnConfiguration(ctx.ColumnType);
            var exploreTask = Task.Run(async () => await launcher.LaunchExploration(ctx, conn, config));

            // Register the exploration for future reference.
            var id = explorationRegistry.Register(exploreTask, cts);

            return Ok(new ExploreResult(id, ExplorationStatus.New));
        }

        [HttpGet]
        [Route("result/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Result(
            System.Guid explorationId,
            [FromServices] MetricsPublisher metricsPublisher)
        {
            var explorationStatus = ExplorationStatus.Error;

            try
            {
                explorationStatus = explorationRegistry.GetStatus(explorationId);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return NotFound($"Couldn't find exploration with id {explorationId}.");
            }

            var metrics = metricsPublisher.PublishedMetrics
                    .Select(m => new ExploreResult.Metric(m.Name, m.Metric));

            var result = new ExploreResult(
                        explorationId,
                        explorationStatus,
                        metrics);

            if (explorationStatus != ExplorationStatus.New && explorationStatus != ExplorationStatus.Processing)
            {
                try
                {
                    await explorationRegistry.Remove(explorationId);
                }
                catch (TaskCanceledException)
                {
                    // Do nothing, just log the occurrence.
                    // A TaskCanceledException is expected when the client cancels an exploration.
                    logger.LogInformation($"Exploration {explorationId} was canceled.", null);
                }
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("cancel/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Cancel(System.Guid explorationId)
        {
            try
            {
                explorationRegistry.CancelExploration(explorationId);
                return Ok(true);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return BadRequest($"Couldn't find exploration with id {explorationId}");
            }
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();
    }
}
