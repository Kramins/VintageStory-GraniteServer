using System.Text;
using Granite.Common.Services;
using Granite.Server.Configuration;
using Granite.Server.Extensions;
using Granite.Server.Hubs;
using Granite.Server.Services;
using Granite.Server.Services.Map;
using GraniteServer.Server.HostedServices;
using GraniteServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use port from config
var port = builder.Configuration.GetValue<int>("GraniteServer:Port", 5000);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

// Configure options
builder.Services.Configure<GraniteServerOptions>(options =>
{
    builder.Configuration.GetSection("GraniteServer").Bind(options);
    options.ApplyEnvironmentVariables();
});

// Add database context
var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();
builder.Services.AddGraniteDatabase(builder.Configuration, logger);

// Add memory cache for player name resolution caching
builder.Services.AddMemoryCache();

// Add authentication services
builder.Services.AddScoped<BasicAuthService>();
builder.Services.AddScoped<JwtTokenService>();

// Add business services
builder.Services.AddScoped<ServersService>();
builder.Services.AddScoped<ServerPlayersService>();
builder.Services.AddScoped<ServerConfigService>();
builder.Services.AddScoped<ServerService>();
builder.Services.AddScoped<IServerWorldMapService, ServerWorldMapService>();
builder.Services.AddScoped<IPlayersService, PlayersService>();
builder.Services.AddScoped<IMapDataStorageService, MapDataStorageService>();
builder.Services.AddScoped<IMapRenderingService, MapRenderingService>();

// TODO: need a better caching strategy here
builder.Services.AddSingleton<IMemoryCache, MemoryCache>();

// Add player name resolver for Vintage Story auth server integration
builder
    .Services.AddHttpClient<IPlayerNameResolver, VintageStoryPlayerNameResolver>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

// Add PersistentMessageBusService as singleton (also registers as MessageBusService)
builder.Services.AddSingleton<PersistentMessageBusService>();

// Add MessageBridgeHostedService to process events from the message bus
builder.Services.AddHostedService<MessageBridgeHostedService>();

// Add ServerInitializationHostedService to ensure server entity exists on startup
builder.Services.AddHostedService<ServerInitializationHostedService>();

// Register event handlers
builder.Services.AddEventHandlers();

// Configure Sieve for filtering, sorting, and pagination
builder.Services.Configure<Sieve.Models.SieveOptions>(options =>
{
    options.DefaultPageSize = 20;
    options.MaxPageSize = 100;
});
builder.Services.AddScoped<Sieve.Services.SieveProcessor>();

// Configure JWT authentication using options with environment overrides
var optionsFromConfig =
    builder.Configuration.GetSection("GraniteServer").Get<GraniteServerOptions>()
    ?? throw new InvalidOperationException("GraniteServer configuration section is missing");
optionsFromConfig.ApplyEnvironmentVariables();
var jwtSecret = optionsFromConfig.JwtSecret;
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JwtSecret is not configured");
}

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "GraniteServer",
            ValidateAudience = true,
            ValidAudience = "GraniteServerClient",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // Enable JWT from query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure CORS for ClientApp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000", // Vite dev server (React)
                "http://localhost:5148",
                "https://localhost:7171", // Blazor WebAssembly dev server
                "http://localhost:5500" // Live Server extension
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for SignalR
    });
});

// Add controllers
builder.Services.AddControllers();

// Add SignalR with increased buffer sizes for large messages (e.g., collectibles sync)
builder.Services.AddSignalR(options =>
{
    // Default is 32KB, increase to 10MB to handle large collections
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

// Add OpenAPI/Swagger
// builder.Services.AddOpenApi();

var app = builder.Build();

// Handle exceptions and wrap in JsonApiDocument (must be first to catch all exceptions)
app.UseMiddleware<Granite.Server.Middleware.ExceptionHandlingMiddleware>();

// Enable routing to access route values in middleware
app.UseRouting();

// Enable WebSockets for SignalR connections
app.UseWebSockets();

// Apply CORS before authentication
app.UseCors();

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Validate serverid when present (non-blocking for now)
app.UseMiddleware<Granite.Server.Middleware.ServerIdValidationMiddleware>();

// Serve static files - CRITICAL ORDER
app.UseBlazorFrameworkFiles(); // Add this line
app.UseStaticFiles();

// Map endpoints
app.MapControllers();
app.MapHub<ModHub>("/hub/mod");
app.MapHub<ClientHub>("/hub/client").RequireAuthorization();

app.UseResponseCompression();

// Fallback to index.html for SPA client-side routing - MUST be last
app.MapFallbackToFile("index.html");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) { }
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GraniteServer.Data.GraniteDataContext>();
    var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            migrationLogger.LogInformation(
                "Applying {Count} pending database migrations...",
                pendingMigrations.Count
            );
            context.Database.Migrate();
            migrationLogger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            migrationLogger.LogInformation("Database is up to date");
        }
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "An error occurred while migrating the database");
        throw;
    }
}

app.Run();
