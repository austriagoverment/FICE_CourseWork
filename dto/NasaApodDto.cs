using System.Text.Json.Serialization;

namespace nasa_pictures.Dto;

public class NasaApodDto
{
    [JsonPropertyName("date")] public string? Date { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("explanation")] public string? Explanation { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("media_type")] public string? MediaType { get; set; }
}