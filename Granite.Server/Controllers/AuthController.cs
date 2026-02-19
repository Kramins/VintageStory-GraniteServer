using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Configuration;
using Granite.Server.Services;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.AspNetCore.Http;
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
    [HttpPost("login")]
    public async Task<ActionResult<JsonApiDocument<TokenDTO>>> Login(
        [FromBody] BasicAuthCredentialsDTO credentials
    )
    {
        var user = await _userManager.FindByNameAsync(credentials.Username);
        if (user == null)
        {
            return Unauthorized(new JsonApiDocument<TokenDTO>
            {
                Errors = { new JsonApiError { Code = "INVALID_CREDENTIALS", Message = "Invalid username or password" } }
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            credentials.Password,
            lockoutOnFailure: false
        );

        if (!result.Succeeded)
        {
            return Unauthorized(new JsonApiDocument<TokenDTO>
            {
                Errors = { new JsonApiError { Code = "INVALID_CREDENTIALS", Message = "Invalid username or password" } }
            });
        }

        if (!user.IsApproved)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new JsonApiDocument<TokenDTO>
            {
                Errors = { new JsonApiError { Code = "ACCOUNT_PENDING_APPROVAL", Message = "Your account is pending approval by an administrator" } }
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.GenerateUserToken(credentials.Username, roles);
        return Ok(new JsonApiDocument<TokenDTO>(token));
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<JsonApiDocument<RegisterResponseDTO>>> Register(
        [FromBody] RegisterDTO registerDto
    )
    {
        if (!_options.RegistrationEnabled)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new JsonApiDocument<RegisterResponseDTO>
            {
                Errors = { new JsonApiError { Code = "REGISTRATION_DISABLED", Message = "Registration is currently disabled" } }
            });
        }

        var existingUser = await _userManager.FindByNameAsync(registerDto.Username);
        if (existingUser != null)
        {
            return BadRequest(new JsonApiDocument<RegisterResponseDTO>
            {
                Errors = { new JsonApiError { Code = "USERNAME_TAKEN", Message = "Username already exists" } }
            });
        }

        var user = new ApplicationUser
        {
            UserName = registerDto.Username,
            Email = registerDto.Email,
            IsApproved = !_options.RequireApproval,
            RegisteredAt = DateTime.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(user, registerDto.Password);

        if (!createResult.Succeeded)
        {
            var doc = new JsonApiDocument<RegisterResponseDTO>();
            foreach (var error in createResult.Errors)
                doc.Errors.Add(new JsonApiError { Code = error.Code, Message = error.Description });
            return BadRequest(doc);
        }

        await _userManager.AddToRoleAsync(user, "User");
        _logger.LogInformation("User {Username} registered successfully", registerDto.Username);

        var message = _options.RequireApproval
            ? "Registration successful. Your account is pending approval by an administrator."
            : "User registered successfully";

        return Ok(new JsonApiDocument<RegisterResponseDTO>(new RegisterResponseDTO(message)));
    }

    /// <summary>
    /// Authenticates a mod with an access token and returns a JWT token.
    /// </summary>
    [HttpPost("token")]
    public async Task<ActionResult<JsonApiDocument<TokenDTO>>> ExchangeAccessToken(
        [FromBody] AccessTokenRequestDTO request
    )
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return BadRequest(new JsonApiDocument<TokenDTO>
            {
                Errors = { new JsonApiError { Code = "INVALID_REQUEST", Message = "Access token is required" } }
            });
        }

        var serverEntity = await _dbContext.Servers.FirstOrDefaultAsync(s =>
            s.Id == request.ServerId && s.AccessToken == request.AccessToken
        );

        if (serverEntity == null)
        {
            return Unauthorized(new JsonApiDocument<TokenDTO>
            {
                Errors = { new JsonApiError { Code = "INVALID_TOKEN", Message = "Invalid server or access token" } }
            });
        }

        var token = _jwtTokenService.GenerateModToken(serverEntity.Id, serverEntity.Name);
        return Ok(new JsonApiDocument<TokenDTO>(token));
    }

    /// <summary>
    /// Gets the authentication settings for the server.
    /// </summary>
    [HttpGet("settings")]
    public ActionResult<JsonApiDocument<AuthSettingsDTO>> GetSettings()
    {
        return Ok(new JsonApiDocument<AuthSettingsDTO>(
            new AuthSettingsDTO(_options.AuthenticationType, _options.RegistrationEnabled)
        ));
    }
}
