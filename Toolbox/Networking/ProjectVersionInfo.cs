using System;
using System.Text.Json.Serialization;

namespace Toolbox.Networking;

public class ProjectVersionInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    public class Source
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("uploadedAt")]
        public string? UploadedAt { get; set; }
    }

    [JsonPropertyName("source")]
    public Source? source { get; set; }

    public string? Name => source?.Name;

    public DateTimeOffset? CreatedAt =>
        DateTimeOffset.TryParse(source?.UploadedAt, out var dto)
            ? dto
            : null;

    [JsonPropertyName("isShared")]
    public bool IsShared { get; set; }

    public override string ToString()
    {
        string versionName = string.IsNullOrWhiteSpace(Name) ? "Unnamed version" : Name;
        string created = CreatedAt?.ToLocalTime().ToString("g") ?? "Unknown date";
        return $"{versionName}  |  {created}  |  ID: {Id}";
    }
}
