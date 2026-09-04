using Microsoft.Extensions.FileProviders;
using inFAMOUSReborn.Services;
using inFAMOUSReborn.Models;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
    serverOptions.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps("infamous.pfx", "password123");
    });
});

builder.Services.AddHostedService<DnsProxyService>();
builder.Services.AddSingleton<MissionCatalog>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine($"[Incoming Request] {context.Request.Method} {context.Request.Path}");
    await next();
});

app.Services.GetRequiredService<MissionCatalog>();

var missionsPath = Path.Combine(Directory.GetCurrentDirectory(), "Missions");
if (!Directory.Exists(missionsPath))
{
    Directory.CreateDirectory(missionsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(missionsPath),
    RequestPath = "",
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.MapGet("/", () => "inFAMOUS Reborn API is running!\nMade with love by Adam Stark.");

var _realIdCache = new Dictionary<string, string>();

object? GetPropertyValue(object obj, string propertyName)
{
    if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
    {
        foreach (var prop in jsonElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.GetRawText()
                };
            }
        }
        return null;
    }

    var propInfo = obj.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    if (propInfo != null) return propInfo.GetValue(obj);
    
    var fieldInfo = obj.GetType().GetField(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
    if (fieldInfo != null) return fieldInfo.GetValue(obj);
    
    return null;
}

string GetRealMissionId(Mission m, string worldFolder)
{
    string title = m.Title ?? "Unknown";
    string author = m.Author ?? "Unknown";
    string cacheKey = $"{worldFolder}_{title}_{author}";

    if (_realIdCache.TryGetValue(cacheKey, out string cachedId))
    {
        return cachedId;
    }

    string baseFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Missions", worldFolder);
    string[] possibleNames = { $"{title} - {author}.ium", $"{title}.ium" };
    string foundPath = null;

    foreach (var name in possibleNames)
    {
        string fullPath = Path.Combine(baseFolderPath, name);
        if (File.Exists(fullPath)) { foundPath = fullPath; break; }
        
        string cleanName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        string cleanFullPath = Path.Combine(baseFolderPath, cleanName);
        if (File.Exists(cleanFullPath)) { foundPath = cleanFullPath; break; }
    }

    if (foundPath != null)
    {
        try
        {
            using var fs = new FileStream(foundPath, FileMode.Open, FileAccess.Read);
            long readLength = Math.Min(fs.Length, 512);
            fs.Seek(-readLength, SeekOrigin.End);
            byte[] buffer = new byte[readLength];
            fs.Read(buffer, 0, (int)readLength);
            
            string tail = System.Text.Encoding.ASCII.GetString(buffer);
            
            var match = Regex.Match(tail, @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})", RegexOptions.IgnoreCase);
            if (match.Success) 
            {
                string realId = match.Groups[1].Value.ToLower();
                _realIdCache[cacheKey] = realId;
                return realId;
            }
        }
        catch { }
    }
    
    string fallbackId = !string.IsNullOrWhiteSpace(m.Id) ? m.Id : Guid.NewGuid().ToString();
    _realIdCache[cacheKey] = fallbackId;
    return fallbackId;
}

app.MapGet("/fob/config.json", () => Results.Json(new
{
    mode = "on",
    protover = 2,
    protover_max = 2,
    initiallisturl = "/fob/static/initial.json",
    xpcap_dt_min = 0.0,
    xpcap_dt_max = 600.0,
    xpcap_xp_min = 25.0,
    xpcap_xp_max = 325.0,
    kpcap_dt_min = 0.0,
    kpcap_dt_max = 60.0,
    kpcap_kp_min = 25.0,
    kpcap_kp_max = 100.0,
    enable_search = 1,
    search_request_limit = 10,
    intro_missions_required = 3,
    daily_publish_limit = 5,
    mission_republish_limit = 5,
    messages = new object[]
    {
        new { id = 1, title_tbid = 763, message_tbid = 531067 }
    },
    filesync = new object[]
    {
        new { file = "cache_fob/ugc_message.sprig.xpps_a", url = "/fob/static/packs/ugc_message.sprig.xpps_a", ver = 15 },
        new { file = "cache_fob/ugc_message.sprig.xpp_a", url = "/fob/static/packs/ugc_message.sprig.xpp_a", ver = 15 },
        new { file = "cache_fob/ugc_pack_festival.sprig.xpps_a", url = "/fob/static/packs/ugc_pack_festival.sprig.xpps_a", ver = 4, gprog = "NewGame", name = 530419 },
        new { file = "cache_fob/ugc_pack_festival.sprig.xpp_a", url = "/fob/static/packs/ugc_pack_festival.sprig.xpp_a", ver = 4, gprog = "NewGame", name = 530419 },
        new { file = "cache_fob/ugc_pack_lights.sprig.xpps_a", url = "/fob/static/packs/ugc_pack_lights.sprig.xpps_a", ver = 4, gprog = "NewGame", name = 530750 },
        new { file = "cache_fob/ugc_pack_lights.sprig.xpp_a", url = "/fob/static/packs/ugc_pack_lights.sprig.xpp_a", ver = 4, gprog = "NewGame", name = 530750 },
        new { file = "ugc/missions/tp/ugc_template_assassination.ium", url = "/fob/static/templates/ugc_template_assassination.ium", ver = 7 },
        new { file = "ugc/missions/tp/ugc_template_canon.ium", url = "/fob/static/templates/ugc_template_canon.ium", ver = 7 },
        new { file = "ugc/missions/tp/ugc_template_collectibles.ium", url = "/fob/static/templates/ugc_template_collectibles.ium", ver = 5 },
        new { file = "ugc/missions/tp/ugc_template_defense.ium", url = "/fob/static/templates/ugc_template_defense.ium", ver = 7 },
        new { file = "ugc/missions/tp/ugc_template_parkour.ium", url = "/fob/static/templates/ugc_template_parkour.ium", ver = 6 },
        new { file = "ugc/missions/tp/ugc_template_physics.ium", url = "/fob/static/templates/ugc_template_physics.ium", ver = 5 },
        new { file = "ugc/missions/tp/ugc_template_platforming.ium", url = "/fob/static/templates/ugc_template_platforming.ium", ver = 5 },
        new { file = "ugc/missions/tp/ugc_template_race.ium", url = "/fob/static/templates/ugc_template_race.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_searchandrescue.ium", url = "/fob/static/templates/ugc_template_searchandrescue.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_ringrace.ium", url = "/fob/static/templates/ugc_template_ringrace.ium", ver = 5 },
        new { file = "ugc/missions/tp/ugc_template_targetpractice.ium", url = "/fob/static/templates/ugc_template_targetpractice.ium", ver = 5 }
    },
    task_map = new object[]
    {
        new { task = "task_story_ugc_mission_1", UUID = "1b660e85-7ff9-481e-b084-d6f9d1ec2f01" },
        new { task = "task_story_ugc_mission_2", UUID = "c2473ff3-a89b-4a0e-ba24-28c35e798af6" }
    }
}));

app.MapGet("/i2/config.json", () => Results.Json(new
{
    mode = "on",
    protover = 2,
    protover_max = 2,
    initiallisturl = "/i2/static/initial.json",
    xpcap_dt_min = 0.0,
    xpcap_dt_max = 600.0,
    xpcap_xp_min = 25.0,
    xpcap_xp_max = 325.0,
    kpcap_dt_min = 0.0,
    kpcap_dt_max = 60.0,
    kpcap_kp_min = 25.0,
    kpcap_kp_max = 100.0,
    enable_search = 1,
    search_request_limit = 10,
    intro_missions_required = 5,
    daily_publish_limit = 5,
    mission_republish_limit = 5,
    messages = new object[]
    {
        new { id = 1, title_tbid = 763, message_tbid = 436699 },
        new { id = 2, title_tbid = 443391, message_tbid = 443389 },
        new { id = 3, title_tbid = 443390, message_tbid = 443388 },
        new { id = 4, title_tbid = 443390, message_tbid = 490178 },
        new { id = 5, title_tbid = 493524, message_tbid = 493519 },
        new { id = 6, title_tbid = 493522, message_tbid = 493516 },
        new { id = 7, title_tbid = 493523, message_tbid = 493517 },
        new { id = 8, title_tbid = 531068, message_tbid = 531069 },
        new { id = 9, title_tbid = 443390, message_tbid = 531307 }
    },
    filesync = new object[]
    {
        new { file = "cache/ugc_message.sprig.xpps_a", url = "/i2/static/packs/ugc_message.sprig.xpps_a", ver = 15 },
        new { file = "cache/ugc_message.sprig.xpp_a", url = "/i2/static/packs/ugc_message.sprig.xpp_a", ver = 15 },
        new { file = "cache/ugc_pack_vehicles1.sprig.xpps_a", url = "/i2/static/packs/ugc_pack_vehicles1.sprig.xpps_a", ver = 0, gprog = "NewGame", name = 443385 },
        new { file = "cache/ugc_pack_vehicles1.sprig.xpp_a", url = "/i2/static/packs/ugc_pack_vehicles1.sprig.xpp_a", ver = 0, gprog = "NewGame", name = 443385 },
        new { file = "cache/ugc_pack_foliage1.sprig.xpps_a", url = "/i2/static/packs/ugc_pack_foliage1.sprig.xpps_a", ver = 11, gprog = "NewGame", name = 490170 },
        new { file = "cache/ugc_pack_foliage1.sprig.xpp_a", url = "/i2/static/packs/ugc_pack_foliage1.sprig.xpp_a", ver = 11, gprog = "NewGame", name = 490170 },
        new { file = "cache/ugc_pack_switches.sprig.xpps_a", url = "/i2/static/packs/ugc_pack_switches.sprig.xpps_a", ver = 10, gprog = "NewGame", name = 490171 },
        new { file = "cache/ugc_pack_switches.sprig.xpp_a", url = "/i2/static/packs/ugc_pack_switches.sprig.xpp_a", ver = 10, gprog = "NewGame", name = 490171 },
        new { file = "cache/ugc_pack_lights.sprig.xpps_a", url = "/i2/static/packs/ugc_pack_lights.sprig.xpps_a", ver = 2, gprog = "NewGame", name = 530750 },
        new { file = "cache/ugc_pack_lights.sprig.xpp_a", url = "/i2/static/packs/ugc_pack_lights.sprig.xpp_a", ver = 2, gprog = "NewGame", name = 530750 },
        new { file = "ugc/missions/tp/ugc_template_assassination.ium", url = "/i2/static/templates/ugc_template_assassination.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_battle.ium", url = "/i2/static/templates/ugc_template_battle.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_canon.ium", url = "/i2/static/templates/ugc_template_canon.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_chase.ium", url = "/i2/static/templates/ugc_template_chase.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_collectibles.ium", url = "/i2/static/templates/ugc_template_collectibles.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_defeatall.ium", url = "/i2/static/templates/ugc_template_defeatall.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_defense.ium", url = "/i2/static/templates/ugc_template_defense.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_destroy.ium", url = "/i2/static/templates/ugc_template_destroy.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_escort.ium", url = "/i2/static/templates/ugc_template_escort.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_gangwar.ium", url = "/i2/static/templates/ugc_template_gangwar.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_guard.ium", url = "/i2/static/templates/ugc_template_guard.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_narrative.ium", url = "/i2/static/templates/ugc_template_narrative.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_parkour.ium", url = "/i2/static/templates/ugc_template_parkour.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_physics.ium", url = "/i2/static/templates/ugc_template_physics.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_platforming.ium", url = "/i2/static/templates/ugc_template_platforming.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_race.ium", url = "/i2/static/templates/ugc_template_race.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_ringrace.ium", url = "/i2/static/templates/ugc_template_ringrace.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_securenpc.ium", url = "/i2/static/templates/ugc_template_securenpc.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_targetpractice.ium", url = "/i2/static/templates/ugc_template_targetpractice.ium", ver = 8 },
        new { file = "ugc/missions/tp/ugc_template_searchandrescue.ium", url = "/i2/static/templates/ugc_template_searchandrescue.ium", ver = 2 }
    }
}));

app.MapGet("/{fileName}", (HttpContext context, string fileName, MissionCatalog catalog) => 
{
    if (!fileName.EndsWith(".ium", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound();
    }

    string requestedId = fileName.Replace(".ium", "", StringComparison.OrdinalIgnoreCase);

    string worldHeader = context.Request.Headers["world"].ToString();
    string worldFolder = (worldHeader == "1") ? "fob" : "base";

    var mission = catalog.GetAllMissions().FirstOrDefault(m => 
    {
        var mId = GetRealMissionId(m, worldFolder);
        return string.Equals(mId, requestedId, StringComparison.OrdinalIgnoreCase);
    });

    if (mission == null) 
    {
        Console.WriteLine($"[Download] Mission ID not found in memory: {requestedId}");
        return Results.NotFound();
    }

    string title = mission.Title ?? "";
    string author = mission.Author ?? "";

    string baseFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Missions", worldFolder);
    
    string[] possibleNames = {
        $"{title} - {author}.ium",
        $"{title}.ium"
    };

    foreach (var name in possibleNames)
    {
        string fullPath = Path.Combine(baseFolderPath, name);
        if (File.Exists(fullPath))
        {
            Console.WriteLine($"[Download] Serving file: {fullPath}");
            return Results.File(fullPath, contentType: "application/octet-stream", enableRangeProcessing: true);
        }
        
        string cleanName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        string cleanFullPath = Path.Combine(baseFolderPath, cleanName);
        if (File.Exists(cleanFullPath))
        {
            Console.WriteLine($"[Download] Serving cleaned file: {cleanFullPath}");
            return Results.File(cleanFullPath, contentType: "application/octet-stream", enableRangeProcessing: true);
        }
    }

    Console.WriteLine($"[Download] Physical file not found for: {title} - {author} in {worldFolder}");
    return Results.NotFound();
});

var buildMissionObject = (Mission m, string worldFolder) =>
{
    var id = GetRealMissionId(m, worldFolder);

    string title = m.Title ?? "Unknown Mission";
    string caption = m.Caption ?? "";
    string creator = m.Creator ?? "Unknown Author";
    string author = !string.IsNullOrEmpty(m.Author) ? m.Author : creator;
    string slottablepacks = m.SlotTablePacks ?? "";

    string posX = !string.IsNullOrEmpty(m.PositionX) ? m.PositionX : "0.000000";
    string posY = !string.IsNullOrEmpty(m.PositionY) ? m.PositionY : "0.000000";
    string posZ = !string.IsNullOrEmpty(m.PositionZ) ? m.PositionZ : "0.000000";

    return new {
        id = id,
        title = title,
        caption = caption,
        creator = creator,
        author = author,
        dataUrl = $"https://infamous2-release.ps3.online.scea.com/{id}.ium",
        rating = m.Rating,
        playtotal = m.PlayTotal,
        favoritetotal = m.FavoriteTotal,
        recommendtotal = m.RecommendTotal,
        slottablepacks = slottablepacks,
        timeofday = m.TimeOfDay,
        gameProgress = m.GameProgress,
        positionalGameProgress = m.PositionalGameProgress,
        heading = m.Heading,
        position_x = posX,
        position_y = posY,
        position_z = posZ,
        desctagk0 = m.DescTagK0,
        desctagk1 = m.DescTagK1,
        desctagk2 = m.DescTagK2
    };
};

var buildMissionListResponse = (IEnumerable<Mission> missionsToServe, string worldFolder) =>
{
    var resultMissions = missionsToServe.Select(m => buildMissionObject(m, worldFolder)).ToList();

    var responseObj = new
    {
        missions = resultMissions,
        missionCount = resultMissions.Count
    };

    string jsonString = JsonSerializer.Serialize(responseObj, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine("\n=== OUTGOING JSON RESPONSE TO PS3 ===");
    Console.WriteLine(jsonString);
    Console.WriteLine("=====================================\n");

    return Results.Json(responseObj);
};

app.MapGet("/i2/static/initial.json", (MissionCatalog catalog) => 
{
    var initialMissions = catalog.GetAllMissions().Take(32).ToList();
    return buildMissionListResponse(initialMissions, "base");
});

app.MapGet("/fob/static/initial.json", (MissionCatalog catalog) => 
{
    var initialMissions = catalog.GetAllMissions().Take(32).ToList();
    return buildMissionListResponse(initialMissions, "fob");
});

var handleSearch = async (HttpContext context, MissionCatalog catalog) =>
{
    string searchTerm = "";
    if (context.Request.HasFormContentType)
    {
        var form = await context.Request.ReadFormAsync();
        searchTerm = form["q"].FirstOrDefault() ?? form["query"].FirstOrDefault() ?? form["term"].FirstOrDefault() ?? "";
    }

    string worldHeader = context.Request.Headers["world"].ToString();
    string worldFolder = (worldHeader == "1") ? "fob" : "base";

    var allMissions = catalog.GetAllMissions().ToList();
    
    if (!string.IsNullOrEmpty(searchTerm))
    {
        allMissions = allMissions.Where(m => 
            (m.Title?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.Author?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            m.Id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).Take(32).ToList();
    }
    else
    {
        allMissions = allMissions.Take(32).ToList();
    }

    return buildMissionListResponse(allMissions, worldFolder);
};

app.MapPost("/api/missions/search/index.json", handleSearch);
app.MapGet("/api/missions/search/index.json", handleSearch);

app.MapGet("/api/mission/ugc/{uuid}/header.json", (HttpContext context, string uuid, MissionCatalog catalog) => 
{
    string worldHeader = context.Request.Headers["world"].ToString();
    string worldFolder = (worldHeader == "1") ? "fob" : "base";

    var mission = catalog.GetAllMissions().FirstOrDefault(m => 
        string.Equals(GetRealMissionId(m, worldFolder), uuid, StringComparison.OrdinalIgnoreCase));

    if (mission != null) 
    {
        return Results.Json(new
        {
            missions = new object[] { buildMissionObject(mission, worldFolder) },
            missionCount = 1
        });
    }

    return Results.Json(new
    {
        missions = new object[] {},
        missionCount = 0
    });
});

app.MapPost("/api/ticket/login", () => Results.Json(new
{
    token = "mock-session-token-12345",
    account_id = "123456789",
    psn_name = "AdamStark",
    status = "success"
}));

app.MapGet("/api/ticket/login", () => Results.Json(new
{
    token = "mock-session-token-12345",
    account_id = "123456789",
    psn_name = "AdamStark",
    status = "success"
}));

app.MapGet("/api/missions/my/favorites/index.json", () => Results.Json(new { missions = new object[] {}, missionCount = 0 }));
app.MapGet("/api/missions/my/queue/index.json", () => Results.Json(new { missions = new object[] {}, missionCount = 0 }));
app.MapGet("/api/missions/my/played/index.json", () => Results.Json(new { missions = new object[] {}, missionCount = 0 }));
app.MapGet("/api/missions/my/uploaded/index.json", () => Results.Json(new { missions = new object[] {}, missionCount = 0 }));

app.MapFallback((HttpContext context) => 
{
    return Results.Json(new { status = "ok" });
});

app.Run();