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

namespace Amendment.Server.Tests
{
    public class AmendmentControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Amendment.Program> _factory;

        public AmendmentControllerTests(CustomWebApplicationFactory<Amendment.Program> webApplicationFactory)
        {
            _factory = webApplicationFactory;
        }

        [Fact]
        public async Task GetAction_Returns_ListOfAmendments()
        {
            //Arrange
            var client = _factory.CreateClient();
            //Act
            var response = await client.GetAsync("/api/Amendment");

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<List<AmendmentResponse>>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.IsSuccess);
            Assert.NotNull(jsonResponse.Result);
            Assert.NotEmpty(jsonResponse.Result);
        }

        [Fact]
        public async Task GetLiveAction_Returns_LiveAmendments()
        {
            //Arrange
            var client = _factory.CreateClient();
            //Act
            var response = await client.GetAsync("/api/Amendment/Live");

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AmendmentFullBodyResponse>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.IsSuccess);
            Assert.NotNull(jsonResponse.Result);
            Assert.True(jsonResponse.Result.IsLive);
        }

        [Fact]
        public async Task GetByIdAction_Returns_SelectedAmendmentResponse()
        {
            //Arrange
            var client = _factory.CreateClient();
            //Act
            var response = await client.GetAsync("/api/Amendment/2");

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AmendmentResponse>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.IsSuccess);
            Assert.NotNull(jsonResponse.Result);
            Assert.Equal(2, jsonResponse.Result.Id);
        }

        [Fact]
        public async Task PostAction_Returns_CreatedAmendment()
        {
            //Arrange
            var client = _factory.CreateClient();
            var model = new AmendmentRequest { Title = "Hello", Author = "Adam", LegisId = "1234", Motion = "1234", PrimaryLanguageId = 1, Source = "Source" };
            //Act
            var response = await client.PostAsJsonAsync("/api/Amendment", model);

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AmendmentResponse>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.IsSuccess);
            Assert.NotNull(jsonResponse.Result);
            Assert.Empty(jsonResponse.Result.AmendmentBodies);
            Assert.Equal(model.Title, jsonResponse.Result.Title);
            Assert.Equal(model.Author, jsonResponse.Result.Author);
            Assert.Equal(model.LegisId, jsonResponse.Result.LegisId);
            Assert.Equal(model.Motion, jsonResponse.Result.Motion);
            Assert.Equal(model.PrimaryLanguageId, jsonResponse.Result.PrimaryLanguageId);
            Assert.Equal(model.Source, jsonResponse.Result.Source);
        }

        [Fact]
        public async Task PostAction_Returns_ErrorsWhenValidationFails()
        {
            //Arrange
            var client = _factory.CreateClient();
            var model = new AmendmentRequest {  };
            //Act
            var response = await client.PostAsJsonAsync("/api/Amendment", model);

            //Assert
            Assert.NotNull(response);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest);
            var jsonResponse = await response.Content.ReadFromJsonAsync<ApiFailedResult>();
            Assert.NotNull(jsonResponse);
            Assert.False(jsonResponse.IsSuccess);
            Assert.Equal(3, jsonResponse.Errors.Count);
            Assert.Contains(jsonResponse.Errors, x => x.Name == "Title");
            Assert.Contains(jsonResponse.Errors, x => x.Name == "Author");
            Assert.Contains(jsonResponse.Errors, x => x.Name == "PrimaryLanguageId");
        }

        [Fact]
        public async Task PutAction_Returns_UpdatedAmendment()
        {
            //Arrange
            var client = _factory.CreateClient();
            var model = new AmendmentRequest
            {
                Title = "Updated",
                Author = "Updated",
                Motion = "Updated",
                LegisId = "Updated",
                PrimaryLanguageId = 2,
                Source = "Updated",
                IsLive = true
            };

            //Act
            var response = await client.PutAsJsonAsync("/api/Amendment/1", model);

            //Assert
            Assert.NotNull(response);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadFromJsonAsync<Amendment.Shared.ApiSuccessResult<AmendmentResponse>>();
            Assert.NotNull(jsonResponse);
            Assert.True(jsonResponse.IsSuccess);
            Assert.NotNull(jsonResponse.Result);
            Assert.Equal(1, jsonResponse.Result.Id);
            Assert.NotEmpty(jsonResponse.Result.AmendmentBodies);
            Assert.Equal(model.Title, jsonResponse.Result.Title);
            Assert.Equal(model.Author, jsonResponse.Result.Author);
            Assert.Equal(model.LegisId, jsonResponse.Result.LegisId);
            Assert.Equal(model.Motion, jsonResponse.Result.Motion);
            Assert.Equal(model.PrimaryLanguageId, jsonResponse.Result.PrimaryLanguageId);
            Assert.Equal(model.Source, jsonResponse.Result.Source);
            Assert.Equal(model.IsLive, jsonResponse.Result.IsLive);
        }

        [Fact]
        public async Task DeleteAction_Returns_NotFound()
        {
            //Arrange
            var client = _factory.CreateClient();

            //Act
            var response = await client.DeleteAsync("/api/Amendment/2");

            //Assert
            var repo = _factory.Services.GetRequiredService<IAmendmentRepository>();
            var test = await repo.GetByIdAsync(2);
            Assert.Null(test);
        }
    }
}