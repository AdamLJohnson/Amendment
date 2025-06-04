using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace Amendment.Server.Tests;

public class VersionControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public VersionControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Version_Returns_Success()
    {
        // Act
        var response = await _client.GetAsync("/api/version");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var version = await response.Content.ReadFromJsonAsync<VersionResponse>();
        
        Assert.NotNull(version);
        Assert.NotNull(version.Version);
        Assert.NotEmpty(version.Version);
        Assert.True(version.BuildDate > DateTime.MinValue);
        Assert.NotNull(version.Environment);
    }

    [Fact]
    public async Task Get_Version_Returns_Valid_Version_Format()
    {
        // Act
        var response = await _client.GetAsync("/api/version");
        var version = await response.Content.ReadFromJsonAsync<VersionResponse>();

        // Assert
        Assert.NotNull(version);
        
        // Version should be in format like "1.0.0.0"
        var versionParts = version.Version.Split('.');
        Assert.True(versionParts.Length >= 3, "Version should have at least 3 parts");
        
        // Each part should be numeric
        foreach (var part in versionParts)
        {
            Assert.True(int.TryParse(part, out _), $"Version part '{part}' should be numeric");
        }
    }

    [Fact]
    public async Task Get_Version_Is_Anonymous_Accessible()
    {
        // Create a client without authentication
        var factory = new CustomWebApplicationFactory<Program>();
        var anonymousClient = factory.CreateClient();
        
        // Remove any default authentication headers
        anonymousClient.DefaultRequestHeaders.Clear();

        // Act
        var response = await anonymousClient.GetAsync("/api/version");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var version = await response.Content.ReadFromJsonAsync<VersionResponse>();
        Assert.NotNull(version);
    }
}
