using Amendment.Server.Services;
using Amendment.Service.Infrastructure;
using Amendment.Server.Hubs;
using Amendment.Shared.Responses;
using Amendment.Shared.Enums;
using Amendment.Shared;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using Amendment.Server.Mediator.Queries.AmendmentQueries;
using Moq;
using Xunit;
using Amendment.Model.DataModel;

namespace Amendment.Tests.Services
{
    public class ClientNotifierTests
    {
        private readonly Mock<IHubContext<AmendmentHub>> _mockAmendmentHub;
        private readonly Mock<IHubContext<ScreenHub>> _mockScreenHub;
        private readonly Mock<IMediator> _mockMediator;
        private readonly Mock<IHubClients> _mockAmendmentClients;
        private readonly Mock<IHubClients> _mockScreenClients;
        private readonly Mock<IClientProxy> _mockAmendmentClientProxy;
        private readonly Mock<IClientProxy> _mockScreenClientProxy;
        private readonly ClientNotifier _clientNotifier;

        public ClientNotifierTests()
        {
            _mockAmendmentHub = new Mock<IHubContext<AmendmentHub>>();
            _mockScreenHub = new Mock<IHubContext<ScreenHub>>();
            _mockMediator = new Mock<IMediator>();
            _mockAmendmentClients = new Mock<IHubClients>();
            _mockScreenClients = new Mock<IHubClients>();
            _mockAmendmentClientProxy = new Mock<IClientProxy>();
            _mockScreenClientProxy = new Mock<IClientProxy>();

            _mockAmendmentHub.Setup(h => h.Clients).Returns(_mockAmendmentClients.Object);
            _mockScreenHub.Setup(h => h.Clients).Returns(_mockScreenClients.Object);
            _mockAmendmentClients.Setup(c => c.All).Returns(_mockAmendmentClientProxy.Object);
            _mockScreenClients.Setup(c => c.All).Returns(_mockScreenClientProxy.Object);

            _clientNotifier = new ClientNotifier(
                _mockAmendmentHub.Object,
                _mockScreenHub.Object,
                _mockMediator.Object);
        }

        [Fact]
        public async Task SendToAllAsync_AmendmentBodyChange_SendsToAmendmentHub()
        {
            // Arrange
            var amendmentBody = new AmendmentBody
            {
                Id = 1,
                AmendId = 1,
                LanguageId = 1,
                AmendBody = "Test amendment body text",
                IsLive = false
            };

            var serviceObject = new { id = 1, results = new object(), data = amendmentBody, amendment = new object(), user = new object() };

            // Act
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.AmendmentBodyChange, serviceObject);

            // Assert
            _mockAmendmentClientProxy.Verify(
                x => x.SendCoreAsync(
                    "AmendmentBodyUpdate",
                    It.Is<object[]>(args => args.Length == 1 && args[0] is SignalRResponse<AmendmentBodyResponse>),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendToAllAsync_AmendmentBodyChange_SendsToScreenHubWhenAmendmentIsLive()
        {
            // Arrange
            var amendmentBody = new AmendmentBody
            {
                Id = 1,
                AmendId = 1,
                LanguageId = 1,
                AmendBody = "Test amendment body text",
                IsLive = false
            };

            var serviceObject = new { id = 1, results = new object(), data = amendmentBody, amendment = new object(), user = new object() };

            // Setup live amendment query to return the same amendment ID
            var liveAmendmentResponse = new AmendmentFullBodyResponse { Id = 1, IsLive = true };
            _mockMediator.Setup(m => m.Send(It.IsAny<GetLiveAmendmentQuery>(), default))
                .ReturnsAsync(new ApiSuccessResult<AmendmentFullBodyResponse>(liveAmendmentResponse));

            // Act
            await _clientNotifier.SendToAllAsync(DestinationHub.Screen, ClientNotifierMethods.AmendmentBodyChange, serviceObject);

            // Assert
            _mockScreenClientProxy.Verify(
                x => x.SendCoreAsync(
                    "AmendmentBodyUpdate",
                    It.Is<object[]>(args => args.Length == 1 && args[0] is SignalRResponse<AmendmentBodyResponse>),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendToAllAsync_AmendmentBodyChange_DoesNotSendToScreenHubWhenAmendmentIsNotLive()
        {
            // Arrange
            var amendmentBody = new AmendmentBody
            {
                Id = 1,
                AmendId = 1,
                LanguageId = 1,
                AmendBody = "Test amendment body text",
                IsLive = false
            };

            var serviceObject = new { id = 1, results = new object(), data = amendmentBody, amendment = new object(), user = new object() };

            // Setup live amendment query to return a different amendment ID
            var liveAmendmentResponse = new AmendmentFullBodyResponse { Id = 2, IsLive = true };
            _mockMediator.Setup(m => m.Send(It.IsAny<GetLiveAmendmentQuery>(), default))
                .ReturnsAsync(new ApiSuccessResult<AmendmentFullBodyResponse>(liveAmendmentResponse));

            // Act
            await _clientNotifier.SendToAllAsync(DestinationHub.Screen, ClientNotifierMethods.AmendmentBodyChange, serviceObject);

            // Assert
            _mockScreenClientProxy.Verify(
                x => x.SendCoreAsync(
                    "AmendmentBodyUpdate",
                    It.IsAny<object[]>(),
                    default),
                Times.Never);
        }

        [Fact]
        public async Task SendToAllAsync_ClearScreens_SendsToBothHubs()
        {
            // Act
            await _clientNotifier.SendToAllAsync(DestinationHub.Amendment, ClientNotifierMethods.ClearScreens, new object());

            // Assert
            _mockAmendmentClientProxy.Verify(
                x => x.SendCoreAsync("ClearScreens", It.IsAny<object[]>(), default),
                Times.Once);
        }
    }
}
