using Granite.Common.Dto;
using Granite.Server.Configuration;
using Granite.Server.Services;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Granite.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly GraniteServerOptions _options;
    private readonly GraniteDataContext _dbContext;

    public AuthController(
        ILogger<AuthController> logger,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IOptions<GraniteServerOptions> options,
        GraniteDataContext dbContext
    )
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
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
    public async Task<ActionResult<TokenDTO>> Login([FromBody] BasicAuthCredentialsDTO credentials)
    {
        var user = await _userManager.FindByNameAsync(credentials.Username);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            credentials.Password,
            lockoutOnFailure: false
        );

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var token = _jwtTokenService.GenerateUserToken(credentials.Username, "Admin");
        return Ok(token);
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="registerDto">Registration details</param>
    /// <returns>Success message or validation errors</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
    {
        var existingUser = await _userManager.FindByNameAsync(registerDto.Username);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Username already exists" });
        }

        var user = new ApplicationUser
        {
            UserName = registerDto.Username,
            Email = registerDto.Email,
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(
                new { message = "Registration failed", errors = result.Errors.Select(e => e.Description) }
            );
        }

        _logger.LogInformation("User {Username} registered successfully", registerDto.Username);
        return Ok(new { message = "User registered successfully" });
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
