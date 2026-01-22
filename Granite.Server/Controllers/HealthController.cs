using Granite.Common.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthDTO> Get()
    {
        return Ok(new HealthDTO());
    }
}
