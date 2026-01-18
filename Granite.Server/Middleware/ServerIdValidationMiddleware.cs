using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Granite.Server.Middleware
{
    public class ServerIdValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public ServerIdValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Attempt to read the serverid route value if present
            var routeData = context.GetRouteData();
            if (routeData?.Values != null && routeData.Values.TryGetValue("serverid", out var value))
            {
                var serverIdString = value?.ToString();
                // For now, just validate format if present; always allow
                if (serverIdString != null && Guid.TryParse(serverIdString, out var guid))
                {
                    // Optionally stash parsed Guid for downstream usage
                    context.Items["ServerId"] = guid;
                }
                else
                {
                    // If invalid, still allow for now per requirements
                    // Future: short-circuit with 400/404
                }
            }

            await _next(context);
        }
    }
}
