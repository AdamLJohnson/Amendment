namespace Amendment.Shared.Responses;

public class VersionResponse
{
    public string Version { get; set; } = string.Empty;
    public DateTime BuildDate { get; set; }
    public string BuildNumber { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}
