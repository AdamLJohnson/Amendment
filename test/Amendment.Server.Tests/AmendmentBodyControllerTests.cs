using Amendment.Shared.Responses;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net;
using System.Net.Http.Json;
using Amendment.Repository;
using Amendment.Shared.Requests;
using Newtonsoft.Json;
using Blazorise.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Amendment.Shared;
using Amendment.Shared.Enums;

namespace Amendment.Server.Tests
{
    public class AmendmentBodyControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Amendment.Program> _factory;

        public AmendmentBodyControllerTests(CustomWebApplicationFactory<Amendment.Program> webApplicationFactory)
        {
            _factory = webApplicationFactory;
        }

        [Fact]
        public async Task GetAction_Returns_ListOfAmendmentBodies()
        {
            //Arrange
            var client = _factory.CreateClient();
            //Act
            var response = await client.GetAsync("/api/Amendment/1/Body");

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<List<AmendmentBodyResponse>>>();
            Assert.True(jsonResponse?.IsSuccess);
            Assert.NotNull(jsonResponse?.Result);
            Assert.NotEmpty(jsonResponse.Result);
        }

        [Fact]
        public async Task GetByIdAction_Returns_SelectedAmendmentBodyResponse()
        {
            //Arrange
            var client = _factory.CreateClient();
            var repo = _factory.Services.GetRequiredService<IAmendmentBodyRepository>();
            var testSetup = (await repo.GetAllAsync()).First(x => x.AmendId == 3);
            //Act
            var response = await client.GetAsync($"/api/Amendment/3/Body/{testSetup.Id}");

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AmendmentBodyResponse>>();
            Assert.True(jsonResponse?.IsSuccess);
            Assert.NotNull(jsonResponse?.Result);
            Assert.Equal(testSetup.Id, jsonResponse.Result.Id);
        }

        [Fact]
        public async Task PostAction_Returns_CreatedAmendmentBody()
        {
            //Arrange
            var client = _factory.CreateClient();
            var model = new AmendmentBodyRequest
            {
                AmendBody = "Hello",
                AmendStatus = AmendmentBodyStatus.Ready,
                IsLive = false,
                LanguageId = 1,
            };
            //Act
            var response = await client.PostAsJsonAsync("/api/Amendment/1/Body", model);

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AmendmentBodyResponse>>();
            Assert.True(jsonResponse?.IsSuccess);
            Assert.NotNull(jsonResponse?.Result);
            Assert.Equal(model.AmendBody, jsonResponse.Result.AmendBody);
            Assert.Equal(model.AmendStatus, jsonResponse.Result.AmendStatus);
            Assert.Equal(model.IsLive, jsonResponse.Result.IsLive);
            Assert.Equal(model.LanguageId, jsonResponse.Result.LanguageId);
        }

        [Fact]
        public async Task PostAction_Returns_ErrorsWhenValidationFails()
        {
            //Arrange
            var client = _factory.CreateClient();
            var model = new AmendmentBodyRequest {  };
            //Act
            var response = await client.PostAsJsonAsync("/api/Amendment/1/Body", model);

            //Assert
            Assert.NotNull(response);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest);
            var jsonResponse = await response.Content.ReadFromJsonAsync<ApiFailedResult>();
            Assert.NotNull(jsonResponse);
            Assert.False(jsonResponse.IsSuccess);
            Assert.Equal(2, jsonResponse.Errors.Count);
            Assert.Contains(jsonResponse.Errors, x => x.Name == "AmendBody");
            Assert.Contains(jsonResponse.Errors, x => x.Name == "LanguageId");
        }

        [Fact]
        public async Task PutAction_Returns_UpdatedAmendmentBody()
        {
            //Arrange
            var client = _factory.CreateClient();
            var repo = _factory.Services.GetRequiredService<IAmendmentBodyRepository>();
            var testSetup = (await repo.GetAllAsync()).First(x => x.AmendId == 4);
            var model = new AmendmentBodyRequest
            {
                AmendBody = "Hello 222",
                AmendStatus = AmendmentBodyStatus.Draft,
                IsLive = true,
                LanguageId = 2,
            };

            //Act
            var response = await client.PutAsJsonAsync($"/api/Amendment/4/Body/{testSetup.Id}", model);

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AmendmentBodyResponse>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.IsSuccess);
            Assert.NotNull(jsonResponse.Result);
            Assert.Equal(testSetup.Id, jsonResponse.Result.Id);
            Assert.Equal(model.AmendBody, jsonResponse.Result.AmendBody);
            Assert.Equal(model.AmendStatus, jsonResponse.Result.AmendStatus);
            Assert.Equal(model.IsLive, jsonResponse.Result.IsLive);
            Assert.Equal(model.LanguageId, jsonResponse.Result.LanguageId);
        }

        [Fact]
        public async Task DeleteAction_Returns_NotFound()
        {
            //Arrange
            var client = _factory.CreateClient();
            var repo = _factory.Services.GetRequiredService<IAmendmentBodyRepository>();
            var testSetup = (await repo.GetAllAsync()).First(x => x.AmendId == 5);

            //Act
            var response = await client.DeleteAsync($"/api/Amendment/5/Body/{testSetup.Id}");

            //Assert

            var test = await repo.GetByIdAsync(testSetup.Id);
            Assert.Null(test);
        }
    }
}