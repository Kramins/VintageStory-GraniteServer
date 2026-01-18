using System.Text;
using Granite.Server.Configuration;
using Granite.Server.Extensions;
using Granite.Server.Hubs;
using Granite.Server.Services;
using GraniteServer.Server.HostedServices;
using GraniteServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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

// Add authentication services
builder.Services.AddScoped<BasicAuthService>();
builder.Services.AddScoped<JwtTokenService>();

// Add business services
builder.Services.AddScoped<ServersService>();
builder.Services.AddScoped<ServerPlayersService>();

// Add MessageBusService as singleton
builder.Services.AddSingleton<MessageBusService>();

// Add MessageBridgeHostedService to process events from the message bus
builder.Services.AddHostedService<MessageBridgeHostedService>();

// Add ServerInitializationHostedService to ensure server entity exists on startup
builder.Services.AddHostedService<ServerInitializationHostedService>();

// Register event handlers
builder.Services.AddEventHandlers();

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

// Configure CORS for ClientApp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "https://localhost:3000") // Vite dev server
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for SignalR
    });
});

// Add controllers
builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR();

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

// Handle exceptions and wrap in JsonApiDocument (must be first to catch all exceptions)
app.UseMiddleware<Granite.Server.Middleware.ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable routing to access route values in middleware
app.UseRouting();

// Apply CORS before authentication
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Validate serverid when present (non-blocking for now)
app.UseMiddleware<Granite.Server.Middleware.ServerIdValidationMiddleware>();

// Serve static files from ClientApp/dist
app.UseStaticFiles();
app.UseDefaultFiles();

// Map endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<GraniteHub>("/hub/mod");
    endpoints.MapHub<ClientHub>("/hub/client");
});

// Fallback to index.html for SPA routing (must be after MapControllers)
app.MapFallbackToFile("index.html");

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
