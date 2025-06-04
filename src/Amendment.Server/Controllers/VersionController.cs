using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Amendment.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VersionController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public VersionController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Try to get version from assembly metadata first (injected during build)
        var version = GetAssemblyMetadata(assembly, "Version") ??
                     assembly.GetName().Version?.ToString() ??
                     "1.0.0.0";

        // Get build date from assembly metadata
        var buildDate = GetBuildDate(assembly);

        // Get build number from assembly metadata, environment, or assembly version
        var buildNumber = GetAssemblyMetadata(assembly, "BuildNumber") ??
                         Environment.GetEnvironmentVariable("BUILD_NUMBER") ??
                         assembly.GetName().Version?.Build.ToString() ??
                         "0";

        var response = new VersionResponse
        {
            Version = version,
            BuildDate = buildDate,
            BuildNumber = buildNumber,
            Environment = _environment.EnvironmentName
        };

        return Ok(response);
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        // Try to get build date from assembly attributes
        var buildDateAttributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        var buildDateAttribute = buildDateAttributes
            .FirstOrDefault(attr => attr.Key == "BuildDate");

        if (buildDateAttribute != null && DateTime.TryParse(buildDateAttribute.Value, out var buildDate))
        {
            return buildDate;
        }

        // Fallback to file creation time
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
        {
            return System.IO.File.GetCreationTimeUtc(location);
        }

        // Final fallback
        return DateTime.UtcNow;
    }

    private static string? GetAssemblyMetadata(Assembly assembly, string key)
    {
        var metadataAttributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        var metadataAttribute = metadataAttributes
            .FirstOrDefault(attr => attr.Key == key);

        return metadataAttribute?.Value;
    }
}
