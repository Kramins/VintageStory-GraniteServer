using Granite.Common.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    public ActionResult<HealthDTO> Get()
    {
        return Ok(new HealthDTO());
    }
}
