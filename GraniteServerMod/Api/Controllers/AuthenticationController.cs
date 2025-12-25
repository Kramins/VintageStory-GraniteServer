using System;
using System.Threading.Tasks;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.Reflection;
using GenHTTP.Modules.Webservices;
using GraniteServer.Api.Models;
using GraniteServer.Api.Services;

namespace GraniteServer.Api.Controllers;

public class AuthenticationController
{
    private readonly BasicAuthService _basicAuthService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly GraniteServerConfig _config;

    public AuthenticationController(
        BasicAuthService basicAuthService,
        JwtTokenService jwtTokenService,
        GraniteServerConfig config
    )
    {
        _basicAuthService = basicAuthService;
        _jwtTokenService = jwtTokenService;
        _config = config;
    }

    [ResourceMethod(RequestMethod.Post, "/login")]
    public async Task<Result<TokenDTO>> Login(BasicAuthCredentialsDTO credentials)
    {
        if (_basicAuthService.ValidateCredentials(credentials.Username, credentials.Password))
        {
            var token = _jwtTokenService.GenerateToken(credentials.Username, "Admin");

            return new Result<TokenDTO>(token);
        }
        else
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }
    }

    [ResourceMethod(RequestMethod.Get, "/settings")]
    public Result<AuthSettingsDTO> GetAuthSettings()
    {
        var authSettings = new AuthSettingsDTO(_config.AuthenticationType);
        return new Result<AuthSettingsDTO>(authSettings);
    }
}
