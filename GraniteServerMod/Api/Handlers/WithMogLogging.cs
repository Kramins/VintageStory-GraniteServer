using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GraniteServer.Api.Services;
using Vintagestory.API.Common;

namespace GraniteServerMod.Api.Handlers;

public class WithMogLogging : IServerCompanion
{
    private readonly ILogger _logger;
    private readonly JwtTokenService _jwtTokenService;

    public WithMogLogging(ILogger logger, JwtTokenService jwtTokenService)
    {
        _logger = logger;
        _jwtTokenService = jwtTokenService;
    }

    public void OnRequestHandled(IRequest request, IResponse response)
    {
        try
        {
            // Apache Combined Log Format:
            var remoteHost = request.Client?.IPAddress?.ToString() ?? "-";
            var ident = "-"; // not available
            var user = "-"; // not available

            var now = DateTimeOffset.Now;
            var timestamp = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2}:{3}:{4}:{5} {6}",
                now.Day.ToString("00"),
                now.ToString("MMM", CultureInfo.InvariantCulture),
                now.Year,
                now.Hour.ToString("00"),
                now.Minute.ToString("00"),
                now.Second.ToString("00"),
                now.ToString("zzz").Replace(":", "")
            );

            var method =
                request.Method.KnownMethod?.ToString().ToUpperInvariant()
                ?? request.Method.RawMethod.ToUpperInvariant();
            var path = request.Target?.Path?.ToString() ?? "/";
            var protocol = request.Client?.Protocol?.ToString().ToUpperInvariant() ?? "HTTP";

            var statusName = response.Status.ToString();
            var statusCode = statusName switch
            {
                "OK" => 200,
                "Created" => 201,
                "NoContent" => 204,
                "BadRequest" => 400,
                "Unauthorized" => 401,
                "Forbidden" => 403,
                "NotFound" => 404,
                "MethodNotAllowed" => 405,
                "Conflict" => 409,
                "InternalServerError" => 500,
                "NotImplemented" => 501,
                "BadGateway" => 502,
                "ServiceUnavailable" => 503,
                "GatewayTimeout" => 504,
                _ => 0,
            };
            var bytes = "-"; // response size not provided by GenHTTP here

            var referer = request.Referer ?? "-";
            var userAgent = request.UserAgent ?? "-";

            if (request.Headers.TryGetValue("authorization", out var authorization))
            {
                if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                {
                    var token = authorization.Substring("Bearer ".Length).Trim();
                    try
                    {
                        var jwt = _jwtTokenService.ValidateToken(token);
                        if (jwt != null)
                        {
                            user = jwt.Identity?.Name ?? "-";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[WebAPI] Error validating JWT for logging: {ex}");
                    }
                }
            }

            var requestLine = $"{method} {path} {protocol}";
            var line =
                $"[Request] {remoteHost} {ident} {user} [{timestamp}] \"{requestLine}\" {statusCode} {bytes} \"{referer}\" \"{userAgent}\"";

            _logger.Log(EnumLogType.Notification, line);
        }
        catch (Exception ex)
        {
            _logger.Error($"[Request] Error logging request: {ex}");
        }
    }

    public void OnServerError(ServerErrorScope scope, IPAddress? client, Exception error)
    {
        _logger.Error(
            $"[Request] Server error ({scope}) from {client?.ToString() ?? "unknown client"}: {error}"
        );
    }

    // No additional helpers needed with available IRequest/IResponse fields
}
