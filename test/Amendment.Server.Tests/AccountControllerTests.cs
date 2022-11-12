using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net;
using System.Net.Http.Json;
using Newtonsoft.Json;

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
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AccountLoginResponse>>();
            Assert.True(jsonResponse.IsSuccess);
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
            var str = await response.Content.ReadAsStringAsync();
            var j = Newtonsoft.Json.JsonConvert.DeserializeObject<Amendment.Shared.ApiFailedResult>(str, new JsonSerializerSettings{ CheckAdditionalContent = true});
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiFailedResult>();
            Assert.False(jsonResponse.IsSuccess);
            Assert.True(jsonResponse.Errors.Count() == 1);
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