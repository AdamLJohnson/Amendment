using System.Net;
using System.Text;
using System.Text.Json;
using Amendment.Client.Repository;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using Xunit;

namespace Amendment.Client.Repository.Tests
{
    public class AmendmentRepositoryErrorHandlingTests
    {
        private readonly ILogger<AmendmentRepository> _logger;
        private readonly INotificationServiceWrapper _notificationServiceWrapper;
        private readonly IJSRuntime _jsRuntime;

        public AmendmentRepositoryErrorHandlingTests()
        {
            _logger = Substitute.For<ILogger<AmendmentRepository>>();
            _notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            _jsRuntime = Substitute.For<IJSRuntime>();
        }

        [Fact]
        public async Task PostWithErrorHandlingAsync_Returns_Success_When_Request_Succeeds()
        {
            // Arrange
            var expectedResponse = new AmendmentResponse { Id = 1, Title = "Test Amendment" };
            var apiResult = new ApiSuccessResult<AmendmentResponse>(expectedResponse);
            var jsonResponse = JsonSerializer.Serialize(apiResult);
            
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
            
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            
            var amendmentRepository = new AmendmentRepository(_logger, client, _notificationServiceWrapper, _jsRuntime);
            var request = new AmendmentRequest { Title = "Test Amendment" };

            // Act
            var (result, errorMessage) = await amendmentRepository.PostWithErrorHandlingAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Amendment", result.Title);
            Assert.Null(errorMessage);
        }

        [Fact]
        public async Task PostWithErrorHandlingAsync_Returns_Error_When_Validation_Fails()
        {
            // Arrange
            var validationErrors = new List<ValidationError>
            {
                new ValidationError { Name = "Title", Message = "Title is required" }
            };
            var apiResult = new ApiFailedResult<AmendmentResponse>(validationErrors);
            var jsonResponse = JsonSerializer.Serialize(apiResult);
            
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
            
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            
            var amendmentRepository = new AmendmentRepository(_logger, client, _notificationServiceWrapper, _jsRuntime);
            var request = new AmendmentRequest { Title = "" };

            // Act
            var (result, errorMessage) = await amendmentRepository.PostWithErrorHandlingAsync(request);

            // Assert
            Assert.Null(result);
            Assert.Equal("Title is required", errorMessage);
        }

        [Fact]
        public async Task PutWithErrorHandlingAsync_Returns_Success_When_Request_Succeeds()
        {
            // Arrange
            var expectedResponse = new AmendmentResponse { Id = 1, Title = "Updated Amendment" };
            var apiResult = new ApiSuccessResult<AmendmentResponse>(expectedResponse);
            var jsonResponse = JsonSerializer.Serialize(apiResult);
            
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
            
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            
            var amendmentRepository = new AmendmentRepository(_logger, client, _notificationServiceWrapper, _jsRuntime);
            var request = new AmendmentRequest { Title = "Updated Amendment" };

            // Act
            var (result, errorMessage) = await amendmentRepository.PutWithErrorHandlingAsync(1, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Updated Amendment", result.Title);
            Assert.Null(errorMessage);
        }

        [Fact]
        public async Task PutWithErrorHandlingAsync_Returns_Error_When_Validation_Fails()
        {
            // Arrange
            var validationErrors = new List<ValidationError>
            {
                new ValidationError { Name = "Author", Message = "Author is required" }
            };
            var apiResult = new ApiFailedResult<AmendmentResponse>(validationErrors);
            var jsonResponse = JsonSerializer.Serialize(apiResult);
            
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
            
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            
            var amendmentRepository = new AmendmentRepository(_logger, client, _notificationServiceWrapper, _jsRuntime);
            var request = new AmendmentRequest { Title = "Test", Author = "" };

            // Act
            var (result, errorMessage) = await amendmentRepository.PutWithErrorHandlingAsync(1, request);

            // Assert
            Assert.Null(result);
            Assert.Equal("Author is required", errorMessage);
        }
    }
}
