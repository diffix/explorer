namespace Explorer.Api.Controllers
{
    using System.Net.Mime;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class ExploreController : ControllerBase
    {
        private readonly ILogger<ExploreController> logger;

        public ExploreController(ILogger<ExploreController> logger)
        {
            this.logger = logger;
        }

        [HttpPost]
        [Route("explore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Explore(Models.ExploreParams data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return Ok(new Models.NotImplementedError() { Data = data });
        }

        [Route("/{**catchall}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult OtherActions() => NotFound();
    }
}
