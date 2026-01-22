using Granite.Common.Dto;
using Granite.Server.Configuration;
using Granite.Server.Services;
using GraniteServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Granite.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BasicAuthService _basicAuthService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly GraniteServerOptions _options;
    private readonly GraniteDataContext _dbContext;

    public AuthController(
        BasicAuthService basicAuthService,
        JwtTokenService jwtTokenService,
        IOptions<GraniteServerOptions> options,
        GraniteDataContext dbContext
    )
    {
        _basicAuthService = basicAuthService;
        _jwtTokenService = jwtTokenService;
        _options = options.Value;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Authenticates a user with basic auth credentials and returns a JWT token.
    /// </summary>
    /// <param name="credentials">Basic authentication credentials</param>
    /// <returns>JWT token if authentication successful</returns>
    [HttpPost("login")]
    public ActionResult<TokenDTO> Login([FromBody] BasicAuthCredentialsDTO credentials)
    {
        if (!_basicAuthService.ValidateCredentials(credentials.Username, credentials.Password))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var token = _jwtTokenService.GenerateUserToken(credentials.Username, "Admin");
        return Ok(token);
    }

    /// <summary>
    /// Authenticates a mod with an access token and returns a JWT token.
    /// </summary>
    /// <param name="request">Access token request</param>
    /// <returns>JWT token if access token is valid</returns>
    [HttpPost("token")]
    public async Task<ActionResult<TokenDTO>> ExchangeAccessToken(
        [FromBody] AccessTokenRequestDTO request
    )
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return BadRequest(new { message = "Access token is required" });
        }

        // Validate token and server id against database

        var serverEntity = await _dbContext.Servers.FirstOrDefaultAsync(s =>
            s.Id == request.ServerId && s.AccessToken == request.AccessToken
        );

        if (serverEntity == null)
        {
            return Unauthorized(new { message = "Invalid server or access token" });
        }

        var token = _jwtTokenService.GenerateModToken(serverEntity.Id, serverEntity.Name);
        return Ok(token);
    }

    /// <summary>
    /// Gets the authentication settings for the server.
    /// </summary>
    /// <returns>Authentication type configured for the server</returns>
    [HttpGet("settings")]
    public ActionResult<AuthSettingsDTO> GetSettings()
    {
        return Ok(new AuthSettingsDTO(_options.AuthenticationType));
    }
}
