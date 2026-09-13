using System.Text.Json.Serialization;

namespace inFAMOUSReborn.Models;

public class Mission
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("caption")]
    public string Caption { get; set; } = string.Empty;

    [JsonPropertyName("creator")]
    public string Creator { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("playtotal")]
    public int PlayTotal { get; set; }

    [JsonPropertyName("favoritetotal")]
    public int FavoriteTotal { get; set; }

    [JsonPropertyName("recommendtotal")]
    public int RecommendTotal { get; set; }

    [JsonPropertyName("slottablepacks")]
    public string SlotTablePacks { get; set; } = string.Empty;

    [JsonPropertyName("timeofday")]
    public int TimeOfDay { get; set; }

    [JsonPropertyName("gameProgress")]
    public int GameProgress { get; set; }

    [JsonPropertyName("positionalGameProgress")]
    public int PositionalGameProgress { get; set; }

    [JsonPropertyName("heading")]
    public double Heading { get; set; }

    [JsonPropertyName("position_x")]
    public string PositionX { get; set; } = "0.000000";

    [JsonPropertyName("position_y")]
    public string PositionY { get; set; } = "0.000000";

    [JsonPropertyName("position_z")]
    public string PositionZ { get; set; } = "0.000000";

    [JsonPropertyName("desctagk0")]
    public int DescTagK0 { get; set; }

    [JsonPropertyName("desctagk1")]
    public int DescTagK1 { get; set; }

    [JsonPropertyName("desctagk2")]
    public int DescTagK2 { get; set; }

    [JsonPropertyName("dataUrl")]
    public string DataUrl { get; set; } = string.Empty;
}