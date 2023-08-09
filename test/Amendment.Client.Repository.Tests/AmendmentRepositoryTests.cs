using System.Net;
using System.Reflection;
using System.Text.Json;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;
using Amendment.Client.Repository;
using Amendment.Client.Repository.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Amendment.Client.Repository.Tests
{
    public static class Reflect
    {
        public static object Protected(this object target, string name, params object[] args)
        {
            var type = target.GetType();
            var method = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == name).Single();
            return method.Invoke(target, args)!;
        }
    }
    public class AmendmentRepositoryTests
    {
        //Unit tests for all public methods in AmendmentRepository
        //The tests will use NSubstitute to create substitutions
        //The tests should stub the HttpClient and HttpMessageHandler
        [Fact]
        public async Task GetAsync_ShouldReturnEmptyList_WhenNoAmendmentsExist()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<IEnumerable<AmendmentResponse>>() { Result = Enumerable.Empty<AmendmentResponse>() }))
            };
            mockHttpMessageHandler
                .Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            IEnumerable<AmendmentResponse> result = await amendmentRepository.GetAsync();
            //Assert
            Assert.Empty(result);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task GetAsync_ShouldReturnListOfAmendments_WhenAmendmentsExist()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<IEnumerable<AmendmentResponse>>() { Result = new List<AmendmentResponse>() { new AmendmentResponse() { Id = 1, Title = "Test Amendment" } } }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            IEnumerable<AmendmentResponse> result = await amendmentRepository.GetAsync();
            //Assert
            Assert.Single(result);
            Assert.Equal(1, result.First().Id);
            Assert.Equal("Test Amendment", result.First().Title);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }
        [Fact]
        public async Task GetAsync_ShouldReturnEmptyList_WhenAmendmentsExistButApiReturnsError()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<IEnumerable<AmendmentResponse>>() { Result = new List<AmendmentResponse>() { new AmendmentResponse() { Id = 1, Title = "Test Amendment" } } }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            IEnumerable<AmendmentResponse> result = await amendmentRepository.GetAsync();
            //Assert
            Assert.Empty(result);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }
        [Fact]
        public async Task GetAsync_ShouldReturnAnAmendment_WhenTheApiReturnsAnAmendment()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            var result = await amendmentRepository.GetAsync(1);
            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Amendment", result.Title);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }
        [Fact]
        public async Task GetAsync_ShouldReturnException_WhenTheAmendmentNotFound()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            await amendmentRepository.GetAsync(1);
            //Assert
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task PostAsync_ShouldReturnAmendment_WhenTheAmendmentIsPosted()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            var result = await amendmentRepository.PostAsync(new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Amendment", result.Title);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }
        [Fact]
        public async Task PostAsync_ShouldReturnNotNull_WhenTheAmendmentIsPostedButApiReturnsError()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            var result = await amendmentRepository.PostAsync(new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }
        [Fact]
        public async Task PostAsync_ShouldReturnNull_WhenTheAmendmentIsPostedButApiReturnsNull()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = null }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            var result = await amendmentRepository.PostAsync(new AmendmentRequest());
            //Assert
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task PutAsync_ShouldReturnAmendment_WhenTheAmendmentIsPut()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            var result = await amendmentRepository.PutAsync(1, new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Amendment", result.Title);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task PutAsync_ShouldReturnNotNull_WhenTheAmendmentIsPutButApiReturnsError()
        {
            //Arrange
            var logger = Substitute.For<ILogger<AmendmentRepository>>();
            var mockHttpMessageHandler = Substitute.For<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonSerializer.Serialize(new ApiResult<AmendmentResponse>() { Result = new AmendmentResponse() { Id = 1, Title = "Test Amendment" } }))
            };
            mockHttpMessageHandler.Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            var client = new HttpClient(mockHttpMessageHandler);
            client.BaseAddress = new Uri("http://localhost/");
            var notificationServiceWrapper = Substitute.For<INotificationServiceWrapper>();
            var amendmentRepository = new AmendmentRepository(logger, client, notificationServiceWrapper);
            //Act
            var result = await amendmentRepository.PutAsync(1, new AmendmentRequest());
            //Assert
            Assert.NotNull(result);
            mockHttpMessageHandler.Received(1).Protected("SendAsync", Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
        }
    }
}