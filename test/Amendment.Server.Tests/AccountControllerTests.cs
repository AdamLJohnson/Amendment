using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net;
using System.Net.Http.Json;

namespace Amendment.Server.Tests
{
    public class AccountControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Amendment.Program> _factory;

        public AccountControllerTests(CustomWebApplicationFactory<Amendment.Program> webApplicationFactory)
        {
            _factory = webApplicationFactory;
        }

        [Fact]
        public async Task LoginAction_Returns_JWT()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/Account/Login", new Amendment.Shared.Requests.AccountLoginRequest() { Username = "admin", Password = "admin" });

            // Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.Responses.AccountLoginResponse>();
            Assert.True(jsonResponse.IsAuthSuccessful);
        }

        [Fact]
        public async Task LoginAction_WithBadValidationResponse()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/Account/Login", new Amendment.Shared.Requests.AccountLoginRequest() { Username = null, Password = "admin" });
            
            // Assert
            Assert.NotNull(response);
            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
            var jsonResponse = await response.Content.ReadFromJsonAsync<IEnumerable<Amendment.Shared.Responses.AccountLoginResponse>>();
            Assert.True(jsonResponse.Count() == 1);
        }

        [Fact]
        public async Task LoginAction_Returns_Unauthorized_WhenBadUserInfo()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/Account/Login", new Amendment.Shared.Requests.AccountLoginRequest() { Username = "asd", Password = "admin" });

            // Assert
            Assert.NotNull(response);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task LoginAction_Returns_Error_ErrorPageIsAskedFor()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/error");

            // Assert
            Assert.NotNull(response);
        }
    }
}