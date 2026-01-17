using Granite.Common.Dto;
using Granite.Server.Configuration;
using Granite.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Granite.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BasicAuthService _basicAuthService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly GraniteServerOptions _options;

    public AuthController(
        BasicAuthService basicAuthService,
        JwtTokenService jwtTokenService,
        IOptions<GraniteServerOptions> options
    )
    {
        _basicAuthService = basicAuthService;
        _jwtTokenService = jwtTokenService;
        _options = options.Value;
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

        var token = _jwtTokenService.GenerateToken(credentials.Username, "Admin");
        return Ok(token);
    }

    /// <summary>
    /// Authenticates a mod with an access token and returns a JWT token.
    /// </summary>
    /// <param name="request">Access token request</param>
    /// <returns>JWT token if access token is valid</returns>
    [HttpPost("token")]
    public ActionResult<TokenDTO> ExchangeAccessToken([FromBody] AccessTokenRequestDTO request)
    {
        // if (string.IsNullOrWhiteSpace(request.AccessToken))
        // {
        //     return BadRequest(new { message = "Access token is required" });
        // }

        // // TODO: In the future, load valid tokens from database
        // if (request.AccessToken != _options.ModAccessToken)
        // {
        //     return Unauthorized(new { message = "Invalid access token" });
        // }

        var token = _jwtTokenService.GenerateToken("ModClient", "Mod");
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
