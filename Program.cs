using Microsoft.Extensions.FileProviders;
using inFAMOUSReborn.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddHostedService<DnsProxyService>();
builder.Services.AddSingleton<MissionCatalog>();

var app = builder.Build();

// Load missions
app.Services.GetRequiredService<MissionCatalog>();

// Configure static file serving for .ium mission files
var missionsPath = Path.Combine(Directory.GetCurrentDirectory(), "Missions");
if (!Directory.Exists(missionsPath))
{
    Directory.CreateDirectory(missionsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(missionsPath),
    RequestPath = "" 
});

app.MapGet("/", () => "inFAMOUS Reborn API is running!");

// Game server configuration endpoint
app.MapGet("/fob/config.json", () => Results.Json(new
{
    enabled = true,
    motd = "inFAMOUS Reborn by Adam Stark - Connected!",
    maintenance = false,
    version = "1.0"
}));

// Mission list endpoint
app.MapGet("/api/missions", (MissionCatalog catalog) => 
{
    var allMissions = catalog.GetAllMissions().ToList();
    
    return Results.Json(new
    {
        total = allMissions.Count,
        page = 1,
        missions = allMissions
    });
});

// Fallback for unidentified requests to prevent 404 crashes on the PS3
app.MapFallback((HttpContext context) => 
{
    Console.WriteLine($"[Unidentified request from PS3]: {context.Request.Path}");
    return Results.Ok();
});

// Bind to HTTP Port 80
app.Urls.Add("http://0.0.0.0:80");

app.Run();