namespace Explorer.Api.Controllers
{
    using System;
    using System.Net.Mime;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer;
    using Explorer.Api.Authentication;
    using Explorer.Api.Models;
    using Lamar;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using static ExplorationStatusEnum;

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

        [ApiVersion("1.0")]
        [HttpPost]
        [Route("api/v{version:apiVersion}/explore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Explore(
            [FromServices] IContainer rootContainer,
            [FromServices] ExplorationScopeBuilder scopeBuilder,
            [FromServices] JsonApiContextBuilder contextBuilder,
            [FromServices] IAircloakAuthenticationProvider authProvider,
            ExploreParams data)
        {
            // Register the authentication token for this scope.
            if (authProvider is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(data.ApiKey);
            }

            // Launch and register the exploration.
            var exploration = new Exploration(rootContainer, scopeBuilder);
            exploration.Initialise(contextBuilder, data);
            exploration.Run();

            var id = explorationRegistry.Register(data, exploration);

            return Ok(new ExploreResult(id, ExplorationStatus.New, data));
        }

        [ApiVersion("1.0")]
        [HttpGet]
        [Route("api/v{version:apiVersion}/result/{explorationId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Result(
            Guid explorationId)
        {
            if (!explorationRegistry.IsRegistered(explorationId))
            {
                return NotFound($"Couldn't find exploration with id {explorationId}.");
            }

            var (explorationParams, exploration) = explorationRegistry.GetExploration(explorationId);
            var explorationStatus = exploration.Status;

            if (explorationStatus == ExplorationStatus.New || explorationStatus == ExplorationStatus.Validating)
            {
                return Ok(new ExploreResult(explorationId, explorationStatus, explorationParams));
            }

            var exploreResult = new ExploreResult(explorationId, exploration, explorationParams);

            if (!explorationStatus.IsComplete())
            {
                return Ok(exploreResult);
            }

            // exception details are logged
#pragma warning disable CA1031 // catch a more specific allowed exception type, or rethrow the exception;
            try
            {
                // Await the completion task to force exceptions to the surface.
                await exploration.Completion;
            }
            catch (TaskCanceledException)
            {
                // Do nothing, just log the occurrence.
                // A TaskCanceledException is expected when the client cancels an exploration.
                logger.LogInformation($"Exploration {explorationId} was canceled.", null);
            }
            catch (Exception) when (exploration.Completion.Exception != null)
            {
                // Log any other exceptions from the explorer and add them to the response object.
                logger.LogWarning($"Exceptions occurred in the exploration tasks for exploration {explorationId}.");

                foreach (var innerEx in exploration.Completion.Exception.Flatten().InnerExceptions)
                {
                    logger.LogError(innerEx, "Exception occurred in exploration task.", innerEx.Data);
                    exploreResult.AddErrorMessage(innerEx.Message);
                }
            }
#pragma warning restore CA1031 // catch a more specific allowed exception type, or rethrow the exception;

            try
            {
                return Ok(exploreResult);
            }
            finally
            {
                explorationRegistry.Remove(explorationId);
            }
        }

        [ApiVersion("1.0")]
        [HttpGet]
        [Route("api/v{version:apiVersion}/cancel/{explorationId}")]
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
