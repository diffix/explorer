namespace Explorer.Api.Controllers
{
    using System;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer;
    using Explorer.Api.Authentication;
    using Explorer.Api.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class ExploreController : ControllerBase
    {
        private readonly ILogger<ExploreController> logger;
        private readonly ExplorationRegistry explorationRegistry;
        private readonly VersionInfo versionInfo;

        public ExploreController(
            ILogger<ExploreController> logger,
            ExplorationRegistry explorationRegistry,
            VersionInfo versionInfo)
        {
            this.logger = logger;
            this.explorationRegistry = explorationRegistry;
            this.versionInfo = versionInfo;
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

            var apiUri = new Uri(data.ApiUrl);
            var cts = new CancellationTokenSource();
            var conn = connectionBuilder.Build(apiUri, data.DataSource, cts.Token);
            var ctxList = await contextBuilder.Build(apiUri, data.DataSource, data.Table, data.Columns);
            var explorationSettings = ctxList.Select(ctx => (ComponentComposition.ColumnConfiguration(ctx.ColumnType), ctx));
            var exploration = launcher.LaunchExploration(data.DataSource, data.Table, conn, explorationSettings);

            // Register the exploration for future reference.
            var id = explorationRegistry.Register(exploration, cts);

            return Ok(new ExploreResult(id, ExplorationStatus.New, data.DataSource, data.Table, versionInfo));
        }

        [HttpGet]
        [Route("result/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Result(
            Guid explorationId)
        {
            Exploration exploration;
            ExplorationStatus explorationStatus;

            try
            {
                exploration = explorationRegistry.GetExploration(explorationId);
                explorationStatus = explorationRegistry.GetStatus(explorationId);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return NotFound($"Couldn't find exploration with id {explorationId}.");
            }

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

            return Ok(new ExploreResult(explorationId, exploration, versionInfo));
        }

        [HttpGet]
        [Route("cancel/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Cancel(Guid explorationId)
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
