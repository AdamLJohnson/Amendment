using Amendment.Model.DataModel;
using Amendment.Model.Enums;
using Amendment.Server.Mediator.Handlers.AmendmentHandlers;
using Amendment.Server.Mediator.Commands.AmendmentCommands;
using Amendment.Service;
using Amendment.Service.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Amendment.Tests.Services;

public class AmendmentApprovalTests
{
    private readonly Mock<IAmendmentService> _mockAmendmentService;
    private readonly Mock<IAmendmentBodyService> _mockAmendmentBodyService;
    private readonly Mock<IAmendmentCleanupService> _mockCleanupService;
    private readonly Mock<IScreenControlService> _mockScreenControlService;
    private readonly Mock<ILogger<UpdateAmendmentCommandHandler>> _mockLogger;
    private readonly UpdateAmendmentCommandHandler _handler;

    public AmendmentApprovalTests()
    {
        _mockAmendmentService = new Mock<IAmendmentService>();
        _mockAmendmentBodyService = new Mock<IAmendmentBodyService>();
        _mockCleanupService = new Mock<IAmendmentCleanupService>();
        _mockScreenControlService = new Mock<IScreenControlService>();
        _mockLogger = new Mock<ILogger<UpdateAmendmentCommandHandler>>();

        _handler = new UpdateAmendmentCommandHandler(
            _mockLogger.Object,
            _mockAmendmentService.Object,
            _mockCleanupService.Object,
            _mockAmendmentBodyService.Object,
            _mockScreenControlService.Object);
    }

    [Fact]
    public async Task Handle_ApproveAmendmentWithParent_UpdatesParentBodies()
    {
        // Arrange
        var parentAmendment = new Model.DataModel.Amendment
        {
            Id = 1,
            Title = "Parent Amendment",
            IsApproved = true,
            IsLive = true // Parent amendment is live, so screens should be updated
        };

        var childAmendment = new Model.DataModel.Amendment
        {
            Id = 2,
            Title = "Child Amendment",
            ParentAmendmentId = 1,
            IsApproved = false // Will be approved in this test
        };

        var childAmendmentBodies = new List<AmendmentBody>
        {
            new AmendmentBody
            {
                Id = 3,
                AmendId = 2,
                LanguageId = 1,
                AmendBody = "Text with ~~deleted~~ and <u>inserted</u> content",
                AmendStatus = AmendmentBodyStatus.Ready
            }
        };

        var parentAmendmentBodies = new List<AmendmentBody>
        {
            new AmendmentBody
            {
                Id = 1,
                AmendId = 1,
                LanguageId = 1,
                AmendBody = "Original parent text",
                AmendStatus = AmendmentBodyStatus.Ready
            }
        };

        var command = new UpdateAmendmentCommand
        {
            Id = 2,
            SavingUserId = 1,
            IsApproved = true,
            Title = "Child Amendment",
            ParentAmendmentId = 1
        };

        _mockAmendmentService.Setup(x => x.GetAsync(2))
            .ReturnsAsync(childAmendment);

        _mockAmendmentService.Setup(x => x.GetAsync(1))
            .ReturnsAsync(parentAmendment);

        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(2))
            .ReturnsAsync(childAmendmentBodies);

        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1))
            .ReturnsAsync(parentAmendmentBodies);

        _mockCleanupService.Setup(x => x.CleanupAmendmentText("Text with ~~deleted~~ and <u>inserted</u> content"))
            .Returns("Text with inserted content");

        _mockAmendmentBodyService.Setup(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));

        _mockAmendmentService.Setup(x => x.UpdateAsync(It.IsAny<Model.DataModel.Amendment>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify that the child amendment body was cleaned up
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(
            It.Is<AmendmentBody>(b => 
                b.Id == 3 && 
                b.AmendBody == "Text with inserted content"), 
            1), Times.Once);

        // Verify that the parent amendment body was updated with cleaned text
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(
            It.Is<AmendmentBody>(b =>
                b.AmendBody == "Text with inserted content"),
            1), Times.AtLeastOnce);

        // Verify cleanup service was called
        _mockCleanupService.Verify(x => x.CleanupAmendmentText("Text with ~~deleted~~ and <u>inserted</u> content"), Times.Once);

        // Verify screen control service was called to force screen update since parent amendment is live
        _mockScreenControlService.Verify(x => x.UpdateBodyAsync(
            It.IsAny<AmendmentBody>(),
            true), Times.Once);
    }

    [Fact]
    public async Task Handle_ApproveAmendmentWithoutParent_DoesNotUpdateParent()
    {
        // Arrange
        var amendment = new Model.DataModel.Amendment
        {
            Id = 1,
            Title = "Standalone Amendment",
            ParentAmendmentId = null,
            IsApproved = false
        };

        var amendmentBodies = new List<AmendmentBody>
        {
            new AmendmentBody
            {
                Id = 1,
                AmendId = 1,
                LanguageId = 1,
                AmendBody = "Text with ~~deleted~~ and <u>inserted</u> content",
                AmendStatus = AmendmentBodyStatus.Ready
            }
        };

        var command = new UpdateAmendmentCommand
        {
            Id = 1,
            SavingUserId = 1,
            IsApproved = true,
            Title = "Standalone Amendment"
        };

        _mockAmendmentService.Setup(x => x.GetAsync(1))
            .ReturnsAsync(amendment);

        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1))
            .ReturnsAsync(amendmentBodies);

        _mockCleanupService.Setup(x => x.CleanupAmendmentText("Text with ~~deleted~~ and <u>inserted</u> content"))
            .Returns("Text with inserted content");

        _mockAmendmentBodyService.Setup(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));

        _mockAmendmentService.Setup(x => x.UpdateAsync(It.IsAny<Model.DataModel.Amendment>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify that only the amendment's own body was cleaned up
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(
            It.Is<AmendmentBody>(b => 
                b.Id == 1 && 
                b.AmendBody == "Text with inserted content"), 
            1), Times.Once);

        // Verify that GetByAmentmentId was not called for any parent
        _mockAmendmentBodyService.Verify(x => x.GetByAmentmentId(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ApproveAmendmentWithNonLiveParent_DoesNotForceScreenUpdate()
    {
        // Arrange
        var parentAmendment = new Model.DataModel.Amendment
        {
            Id = 1,
            Title = "Parent Amendment",
            IsApproved = true,
            IsLive = false // Parent amendment is not live, so no forced screen updates
        };

        var childAmendment = new Model.DataModel.Amendment
        {
            Id = 2,
            Title = "Child Amendment",
            ParentAmendmentId = 1,
            IsApproved = false
        };

        var childAmendmentBodies = new List<AmendmentBody>
        {
            new AmendmentBody
            {
                Id = 3,
                AmendId = 2,
                LanguageId = 1,
                AmendBody = "Text with ~~deleted~~ and <u>inserted</u> content",
                AmendStatus = AmendmentBodyStatus.Ready
            }
        };

        var parentAmendmentBodies = new List<AmendmentBody>
        {
            new AmendmentBody
            {
                Id = 1,
                AmendId = 1,
                LanguageId = 1,
                AmendBody = "Original parent text",
                AmendStatus = AmendmentBodyStatus.Ready
            }
        };

        var command = new UpdateAmendmentCommand
        {
            Id = 2,
            SavingUserId = 1,
            IsApproved = true,
            Title = "Child Amendment",
            ParentAmendmentId = 1
        };

        _mockAmendmentService.Setup(x => x.GetAsync(2))
            .ReturnsAsync(childAmendment);

        _mockAmendmentService.Setup(x => x.GetAsync(1))
            .ReturnsAsync(parentAmendment);

        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(2))
            .ReturnsAsync(childAmendmentBodies);

        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1))
            .ReturnsAsync(parentAmendmentBodies);

        _mockCleanupService.Setup(x => x.CleanupAmendmentText("Text with ~~deleted~~ and <u>inserted</u> content"))
            .Returns("Text with inserted content");

        _mockAmendmentBodyService.Setup(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));

        _mockAmendmentService.Setup(x => x.UpdateAsync(It.IsAny<Model.DataModel.Amendment>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify that the parent amendment body was updated
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(
            It.Is<AmendmentBody>(b =>
                b.AmendBody == "Text with inserted content"),
            1), Times.AtLeastOnce);

        // Verify screen control service was NOT called since parent amendment is not live
        _mockScreenControlService.Verify(x => x.UpdateBodyAsync(It.IsAny<AmendmentBody>(), true), Times.Never);
    }
}
