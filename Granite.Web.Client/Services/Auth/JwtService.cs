using System.Security.Claims;
using System.Text.Json;

namespace Granite.Web.Client.Services.Auth;

public interface IJwtService
{
    ClaimsPrincipal? DecodeToken(string token);
    bool IsTokenExpired(string token);
}

public class JwtService : IJwtService
{
    public ClaimsPrincipal? DecodeToken(string token)
    {
        try
        {
            // JWT tokens have 3 parts separated by dots
            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            // Decode the payload (middle part)
            var payload = DecodeBase64Url(parts[1]);
            var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
            
            if (claims == null)
                return null;

            var claimsList = new List<Claim>();

            // Extract standard claims
            if (claims.TryGetValue("unique_name", out var uniqueName))
                claimsList.Add(new Claim(ClaimTypes.Name, uniqueName.GetString() ?? "Unknown"));
            else if (claims.TryGetValue("nameid", out var nameid))
                claimsList.Add(new Claim(ClaimTypes.Name, nameid.GetString() ?? "Unknown"));
            else if (claims.TryGetValue("sub", out var sub))
                claimsList.Add(new Claim(ClaimTypes.Name, sub.GetString() ?? "Unknown"));

            if (claims.TryGetValue("role", out var role))
                claimsList.Add(new Claim(ClaimTypes.Role, role.GetString() ?? "User"));

            var identity = new ClaimsIdentity(claimsList, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return true;

            var payload = DecodeBase64Url(parts[1]);
            var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
            
            if (claims == null || !claims.TryGetValue("exp", out var exp))
                return true;

            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());
            return expirationTime <= DateTimeOffset.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private static string DecodeBase64Url(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }
        var bytes = Convert.FromBase64String(output);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}