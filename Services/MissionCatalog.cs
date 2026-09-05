using System.IO.Compression;
using System.Text.Json;
using inFAMOUSReborn.Models;

namespace inFAMOUSReborn.Services;

public class MissionCatalog
{
    private readonly Dictionary<string, Mission> _missions = new();
    private readonly ILogger<MissionCatalog> _logger;

    public MissionCatalog(ILogger<MissionCatalog> logger)
    {
        _logger = logger;
        LoadCatalogs();
    }

    private void LoadCatalogs()
    {
        var missionsDir = PathHelper.GetMissionsDirectory();
        
        var baseFile = Path.Combine(missionsDir, "ugc_missions_base.json.gz");
        var fobFile = Path.Combine(missionsDir, "ugc_missions_fob.json.gz");

        LoadSingleFile(baseFile, "inFAMOUS 2 (Base)");
        LoadSingleFile(fobFile, "inFAMOUS: Festival of Blood (FoB)");

        _logger.LogInformation("==================================================");
        _logger.LogInformation($"[Catalog] Loaded {_missions.Count} missions into memory.");
        _logger.LogInformation("==================================================");
    }

    private void LoadSingleFile(string filePath, string gameName)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"[Catalog] Mission file for {gameName} not found: {filePath}");
            return;
        }

        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            
            var loadedMissions = JsonSerializer.Deserialize<Dictionary<string, Mission>>(gzipStream);
            
            if (loadedMissions != null)
            {
                int count = 0;
                foreach (var mission in loadedMissions)
                {
                    if (_missions.TryAdd(mission.Key, mission.Value))
                    {
                        count++;
                    }
                }
                _logger.LogInformation($"[Catalog] {gameName}: {count} missions loaded.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Catalog] Error with {gameName} mission JSON file ({filePath}): {ex.Message}");
        }
    }

    // Return missions to API endpoint
    public IEnumerable<Mission> GetAllMissions() => _missions.Values;
}