using System.Text.Json.Serialization;

namespace nasa_pictures;

public class ApiResponse
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }

    [JsonPropertyName("hdurl")]
    public string? Hdurl { get; set; }

    [JsonPropertyName("media_type")]
    public string? Media_type { get; set; }

    [JsonPropertyName("service_version")]
    public string? Service_version { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}