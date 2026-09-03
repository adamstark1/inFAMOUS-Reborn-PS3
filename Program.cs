using inFAMOUSReborn.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddHostedService<DnsProxyService>();

var app = builder.Build();

app.MapGet("/", () => "inFAMOUS Reborn API is running!");
app.Run();
