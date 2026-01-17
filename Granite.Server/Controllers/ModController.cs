using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ModController : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<JsonApiDocument<List<ModDTO>>>> GetMods()
    {
        throw new NotImplementedException("GetMods endpoint not yet implemented");
    }
}
