using System.Text.Json;
using inFAMOUSReborn.Models;

namespace inFAMOUSReborn.Services;

public class MissionCatalog
{
    private Dictionary<string, Mission> _missions = new();
    private readonly ILogger<MissionCatalog> _logger;

    public MissionCatalog(ILogger<MissionCatalog> logger)
    {
        _logger = logger;
        LoadCatalog();
    }

    private void LoadCatalog()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "ugc_missions.json");
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"[Catalog] File not found: {filePath}");
            return;
        }

        try
        {
            var jsonText = File.ReadAllText(filePath);
            _missions = JsonSerializer.Deserialize<Dictionary<string, Mission>>(jsonText) ?? new();
            _logger.LogInformation($"[Catalog] Successfully loaded {_missions.Count} missions into memory.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Catalog] Error while processing JSON: {ex.Message}");
        }
    }

    // Give mission list to API
    public IEnumerable<Mission> GetAllMissions() => _missions.Values;
}