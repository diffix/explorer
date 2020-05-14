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
        public IActionResult Explore(
            ExploreParams data,
            [FromServices] ExplorationLauncher launcher)
        {
            var cts = new CancellationTokenSource();
            var exploreTask = launcher.LaunchExploration(data, cts.Token);
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
