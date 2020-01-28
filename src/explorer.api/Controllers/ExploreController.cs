namespace Explorer.Api.Controllers
{
    using System;
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
        private readonly ILogger<ExploreController> logger;
        private readonly JsonApiClient aircloakApiClient;

        public ExploreController(ILogger<ExploreController> logger, JsonApiClient aircloakApiClient)
        {
            this.logger = logger;
            this.aircloakApiClient = aircloakApiClient;
        }

        [HttpPost]
        [Route("explore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Explore(Models.ExploreParams data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var apiSession = new JsonApiSession(
                aircloakApiClient,
                new Uri("https://attack.aircloak.com/api/"),
                data.ApiKey);

            var dataSources = apiSession.GetDataSources().Result;

            if (!dataSources.AsDict.TryGetValue(data.DataSourceName, out var exploreDataSource))
            {
                return BadRequest(); // TODO Return something more descriptive
            }

            if (!exploreDataSource.TableDict.TryGetValue(data.TableName, out var exploreTableMeta))
            {
                return BadRequest(); // TODO Return something more descriptive
            }

            if (!exploreTableMeta.ColumnDict.TryGetValue(data.ColumnName, out var explorerColumnMeta))
            {
                return BadRequest(); // TODO Return something more descriptive
            }

            ColumnExplorer? explorer = explorerColumnMeta.Type switch
            {
                AircloakType.Integer => new IntegerColumnExplorer(apiSession, data),
                AircloakType.Real => new RealColumnExplorer(apiSession, data),
                _ => null,
            };

            if (explorer == null)
            {
                return Ok(new Models.NotImplementedError()
                {
                    Description = $"No exploration strategy implemented for {explorerColumnMeta.Type} columns.",
                    Data = data,
                });
            }

            var results = new System.Collections.Generic.List<ExploreResult>();
            await foreach (var result in explorer.Explore())
            {
                logger.LogInformation($"-------> Explorer status: {result.Status}");
                results.Add(result);
            }

            return Ok(results.FindLast(_ => true));
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();
    }
}
