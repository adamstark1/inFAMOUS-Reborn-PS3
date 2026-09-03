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

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public double Rating { get; set; }

    [JsonPropertyName("playtotal")]
    public int PlayTotal { get; set; }
    
    [JsonPropertyName("dataUrl")]
    public string DataUrl { get; set; } = string.Empty;
}