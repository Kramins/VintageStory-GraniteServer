using GenHTTP.Modules.Authentication.Bearer;

namespace GraniteServer.Api.CustomBearerAuthentication;

public static class CustomBearerAuthentication
{
    /// <summary>
    /// Creates a concern that will read an access token from
    /// the authorization headers and validate it according to
    /// its configuration.
    /// </summary>
    /// <returns>The newly created concern</returns>
    public static CustomBearerAuthenticationConcernBuilder Create() => new();
}
