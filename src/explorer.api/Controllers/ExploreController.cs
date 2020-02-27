namespace Explorer.Api.Controllers
{
    using System.Collections.Concurrent;
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
            this.RegisterApiKey(data.ApiKey);

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

            var explorer = CreateNumericColumnExplorer(explorerColumnMeta.Type, apiClient, data);

            if (explorer == null)
            {
                return Ok(new Models.NotImplementedError
                {
                    Description = $"No exploration strategy implemented for {explorerColumnMeta.Type} columns.",
                    Data = data,
                });
            }

#pragma warning disable CS4014 // Consider applying the 'await' operator to the result of the call.
            explorer.Explore();
#pragma warning restore CS4014 // Consider applying the 'await' operator to the result of the call.

            if (!Explorers.TryAdd(explorer.ExplorationGuid, explorer))
            {
                throw new System.Exception("Failed to store explorer in Dict - This should never happen!");
            }

            return Ok(explorer.LatestResult);
        }

        [HttpGet]
        [Route("result/{exploreId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Result(System.Guid exploreId)
        {
            if (Explorers.TryGetValue(exploreId, out var explorer))
            {
                return Ok(explorer.LatestResult);
            }
            else
            {
                return BadRequest($"Couldn't find explorer with id {exploreId}");
            }
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();

        private static ColumnExplorer? CreateNumericColumnExplorer(AircloakType type, JsonApiClient apiClient, Models.ExploreParams data)
        {
            return type switch
            {
                AircloakType.Integer => new IntegerColumnExplorer(apiClient, data),
                AircloakType.Real => new RealColumnExplorer(apiClient, data),
                AircloakType.Text => new TextColumnExplorer(apiClient, data),
                AircloakType.Bool => new BoolColumnExplorer(apiClient, data),
                _ => null,
            };
        }
    }
}
