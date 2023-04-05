using System.Net;
using System.Text.Json;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Amendment.Client.Repository;
using Amendment.Client.Repository.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Amendment.Client.Repository.Tests
{
    public class AmendmentRepositoryTests
    {
        //Unit tests for all public methods in AmendmentRepository
        //The tests will use Moq to mock the parameters in the constructor
        //The tests should mock the HttpClient and HttpMessageHandler
        [Fact]
        public async Task GetAsync_ShouldReturnEmptyList_WhenNoAmendmentsExist()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<IEnumerable<AmendmentResponse>>() { Result = Enumerable.Empty<AmendmentResponse>() }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            IEnumerable<AmendmentResponse> result = await amendmentRepository.GetAsync();
            //Assert
            Assert.Empty(result);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetAsync_ShouldReturnListOfAmendments_WhenAmendmentsExist()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<IEnumerable<AmendmentResponse>>() { Result = new List<AmendmentResponse>() { new AmendmentResponse() { Id = 1, Title = "Test Amendment" } } }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            IEnumerable<AmendmentResponse> result = await amendmentRepository.GetAsync();
            //Assert
            Assert.Single(result);
            Assert.Equal(1, result.First().Id);
            Assert.Equal("Test Amendment", result.First().Title);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
        [Fact]
        public async Task GetAsync_ShouldReturnEmptyList_WhenAmendmentsExistButApiReturnsError()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<IEnumerable<AmendmentResponse>>() { Result = new List<AmendmentResponse>() { new AmendmentResponse() { Id = 1, Title = "Test Amendment"     }}}))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            IEnumerable<AmendmentResponse> result = await amendmentRepository.GetAsync();
            //Assert
            Assert.Empty(result);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
        [Fact]
        public async Task GetAsync_ShouldReturnAnAmendment_WhenTheApiReturnsAnAmendment()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            var result = await amendmentRepository.GetAsync(1);
            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Amendment", result.Title);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
        [Fact]
        public async Task GetAsync_ShouldReturnException_WhenTheAmendmentNotFound()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            await amendmentRepository.GetAsync(1);
            //Assert
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PostAsync_ShouldReturnAmendment_WhenTheAmendmentIsPosted(){
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            var result = await amendmentRepository.PostAsync(new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Amendment", result.Title);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
        [Fact]
        public async Task PostAsync_ShouldReturnNotNull_WhenTheAmendmentIsPostedButApiReturnsError() 
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            var result = await amendmentRepository.PostAsync(new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
        [Fact]
        public async Task PostAsync_ShouldReturnNull_WhenTheAmendmentIsPostedButApiReturnsNull()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = null }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            var result = await amendmentRepository.PostAsync(new AmendmentRequest());
            //Assert
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PutAsync_ShouldReturnAmendment_WhenTheAmendmentIsPut()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            var result = await amendmentRepository.PutAsync(1, new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Amendment", result.Title);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PutAsync_ShouldReturnNotNull_WhenTheAmendmentIsPutButApiReturnsError()
        {
            //Arrange
            var logger = new Mock<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = new Mock<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger.Object, client, notificationServiceWrapper.Object);
            //Act
            var result = await amendmentRepository.PutAsync(1, new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            mockHttpMessageHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }
}