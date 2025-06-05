using Amendment.Model.DataModel;
using Amendment.Model.Enums;
using Amendment.Server.Handlers.Handlers.AmendmentHandlers;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Amendment.Tests.Services;

public class CloneAmendmentTests
{
    private readonly Mock<IAmendmentService> _mockAmendmentService;
    private readonly Mock<IAmendmentBodyService> _mockAmendmentBodyService;
    private readonly Mock<ILogger<CloneAmendmentCommandHandler>> _mockLogger;
    private readonly CloneAmendmentCommandHandler _handler;

    public CloneAmendmentTests()
    {
        _mockAmendmentService = new Mock<IAmendmentService>();
        _mockAmendmentBodyService = new Mock<IAmendmentBodyService>();
        _mockLogger = new Mock<ILogger<CloneAmendmentCommandHandler>>();
        
        _handler = new CloneAmendmentCommandHandler(
            _mockAmendmentService.Object,
            _mockAmendmentBodyService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidSourceAmendment_ClonesSuccessfully()
    {
        // Arrange
        var sourceAmendment = new Model.DataModel.Amendment
        {
            Id = 1,
            Title = "Original Amendment",
            Author = "Original Author",
            Motion = "WC-1",
            Source = "Conference Floor",
            LegisId = "LEG-001",
            PrimaryLanguageId = 1,
            IsLive = false,
            IsArchived = false,
            IsApproved = true
        };

        var sourceAmendmentBodies = new List<AmendmentBody>
        {
            new AmendmentBody
            {
                Id = 1,
                AmendId = 1,
                LanguageId = 1,
                AmendBody = "Original text with ~~deleted~~ and <u>inserted</u> content",
                AmendStatus = AmendmentBodyStatus.Ready,
                IsLive = false
            },
            new AmendmentBody
            {
                Id = 2,
                AmendId = 1,
                LanguageId = 2,
                AmendBody = "Texto original con ~~eliminado~~ y <u>insertado</u> contenido",
                AmendStatus = AmendmentBodyStatus.Ready,
                IsLive = false
            }
        };

        var command = new CloneAmendmentCommand
        {
            SavingUserId = 1,
            SourceAmendmentId = 1,
            Title = "Cloned Amendment",
            Author = "Clone Author"
        };

        _mockAmendmentService.Setup(x => x.GetAsync(1))
            .ReturnsAsync(sourceAmendment);
        
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1))
            .ReturnsAsync(sourceAmendmentBodies);

        _mockAmendmentService.Setup(x => x.CreateAsync(It.IsAny<Model.DataModel.Amendment>(), It.IsAny<int>()))
            .ReturnsAsync(new Amendment.Service.Infrastructure.OperationResult(Amendment.Service.Infrastructure.OperationType.Create, true));

        _mockAmendmentBodyService.Setup(x => x.CreateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()))
            .ReturnsAsync(new Amendment.Service.Infrastructure.OperationResult(Amendment.Service.Infrastructure.OperationType.Create, true));

        // Mock the final GetAsync call to return the cloned amendment
        var clonedAmendment = new Model.DataModel.Amendment
        {
            Id = 2,
            Title = "Cloned Amendment",
            Author = "Clone Author",
            Motion = "WC-1",
            Source = "Conference Floor",
            LegisId = "LEG-001",
            PrimaryLanguageId = 1,
            ParentAmendmentId = 1,
            IsLive = false,
            IsArchived = false,
            IsApproved = false
        };

        _mockAmendmentService.Setup(x => x.GetAsync(It.Is<int>(id => id != 1)))
            .ReturnsAsync((Model.DataModel.Amendment?)clonedAmendment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        
        // Verify that the amendment was created with correct parent reference
        _mockAmendmentService.Verify(x => x.CreateAsync(
            It.Is<Model.DataModel.Amendment>(a => 
                a.ParentAmendmentId == 1 &&
                a.Title == "Cloned Amendment" &&
                a.Author == "Clone Author" &&
                a.IsApproved == false &&
                a.IsLive == false &&
                a.IsArchived == false), 
            1), Times.Once);

        // Verify that amendment bodies were cloned
        _mockAmendmentBodyService.Verify(x => x.CreateAsync(
            It.Is<AmendmentBody>(b => 
                b.LanguageId == 1 &&
                b.AmendBody == "Original text with ~~deleted~~ and <u>inserted</u> content" &&
                b.IsLive == false), 
            1), Times.Once);

        _mockAmendmentBodyService.Verify(x => x.CreateAsync(
            It.Is<AmendmentBody>(b => 
                b.LanguageId == 2 &&
                b.AmendBody == "Texto original con ~~eliminado~~ y <u>insertado</u> contenido" &&
                b.IsLive == false), 
            1), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentSourceAmendment_ReturnsNotFound()
    {
        // Arrange
        var command = new CloneAmendmentCommand
        {
            SavingUserId = 1,
            SourceAmendmentId = 999
        };

        _mockAmendmentService.Setup(x => x.GetAsync(999))
            .ReturnsAsync((Model.DataModel.Amendment?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, ((Amendment.Shared.ApiFailedResult<Amendment.Shared.Responses.AmendmentResponse>)result).StatusCode);
    }
}
