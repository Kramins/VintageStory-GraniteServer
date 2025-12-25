using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using GenHTTP.Api.Content.Authentication;
using GenHTTP.Api.Protocol;
using Microsoft.IdentityModel.Tokens;

namespace GraniteServer.Api.CustomBearerAuthentication;

public class TokenValidationOptions
{
    public string? Audience { get; set; }

    public string? Issuer { get; set; }

    public bool Lifetime { get; set; } = true;

    public Func<JwtSecurityToken, Task>? CustomValidator { get; set; }

    public Func<IRequest, JwtSecurityToken, ValueTask<IUser?>>? UserMapping { get; set; }

    public Func<JwtSecurityToken, ValueTask<ICollection<SecurityKey>>>? KeyResolver { get; set; }
}
