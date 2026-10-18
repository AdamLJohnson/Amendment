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
            ParentAmendmentId = 1,
            UpdateCurrentAmendmentBodies = true
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
            Title = "Standalone Amendment",
            UpdateCurrentAmendmentBodies = true
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
            ParentAmendmentId = 1,
            UpdateCurrentAmendmentBodies = true
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

    [Fact]
    public async Task Handle_ApproveAmendmentWithParent_UpdateCurrentAmendmentBodiesFalse_DoesNotUpdateCurrentBodies()
    {
        // Arrange
        var parentAmendment = new Model.DataModel.Amendment
        {
            Id = 1,
            Title = "Parent Amendment",
            IsApproved = true,
            IsLive = true
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
            ParentAmendmentId = 1,
            UpdateCurrentAmendmentBodies = false // Don't update current amendment bodies
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

        // Verify that the child amendment body was NOT cleaned up (should keep original formatting)
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(
            It.Is<AmendmentBody>(b =>
                b.Id == 3 &&
                b.AmendBody == "Text with ~~deleted~~ and <u>inserted</u> content"),
            1), Times.Never);

        // Verify that the parent amendment body was still updated with cleaned text
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(
            It.Is<AmendmentBody>(b =>
                b.AmendBody == "Text with inserted content"),
            1), Times.Once);

        // Verify cleanup service was called for parent update
        _mockCleanupService.Verify(x => x.CleanupAmendmentText("Text with ~~deleted~~ and <u>inserted</u> content"), Times.Once);

        // Verify screen control service was called to force screen update since parent amendment is live
        _mockScreenControlService.Verify(x => x.UpdateBodyAsync(
            It.IsAny<AmendmentBody>(),
            true), Times.Once);
    }

    // Phase 0 characterization tests: real AmendmentCleanupService, legacy behavior captured as-is.

    private UpdateAmendmentCommandHandler CreateHandlerWithRealCleanup() =>
        new UpdateAmendmentCommandHandler(
            _mockLogger.Object,
            _mockAmendmentService.Object,
            new AmendmentCleanupService(),
            _mockAmendmentBodyService.Object,
            _mockScreenControlService.Object);

    private void SetupSuccessfulSaves()
    {
        _mockAmendmentBodyService.Setup(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));
        _mockAmendmentService.Setup(x => x.UpdateAsync(It.IsAny<Model.DataModel.Amendment>(), It.IsAny<int>()))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));
    }

    private static AmendmentBody Body(int id, int amendId, int languageId, string text) =>
        new AmendmentBody { Id = id, AmendId = amendId, LanguageId = languageId, AmendBody = text, AmendStatus = AmendmentBodyStatus.Ready };

    private static UpdateAmendmentCommand ApproveCommand(int id, int? parentId, bool updateCurrentBodies) =>
        new UpdateAmendmentCommand
        {
            Id = id,
            SavingUserId = 7,
            IsApproved = true,
            Title = "Child",
            ParentAmendmentId = parentId,
            UpdateCurrentAmendmentBodies = updateCurrentBodies
        };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Characterization_GoldenFixture_ParentBodyReplacedByCleanedChildText(bool updateCurrentBodies)
    {
        var parentBody = Body(10, 1, 1, "We shall proceed with the original plan.");
        _mockAmendmentService.Setup(x => x.GetAsync(2)).ReturnsAsync(new Model.DataModel.Amendment { Id = 2, ParentAmendmentId = 1, IsApproved = false });
        _mockAmendmentService.Setup(x => x.GetAsync(1)).ReturnsAsync(new Model.DataModel.Amendment { Id = 1, IsLive = false });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(2)).ReturnsAsync(new List<AmendmentBody> { Body(20, 2, 1, "We ~~shall~~ <u>will</u> proceed.") });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1)).ReturnsAsync(new List<AmendmentBody> { parentBody });
        SetupSuccessfulSaves();

        var result = await CreateHandlerWithRealCleanup().Handle(ApproveCommand(2, 1, updateCurrentBodies), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("We  will proceed.", parentBody.AmendBody);
        Assert.Equal(7, parentBody.LastUpdatedBy);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.Is<AmendmentBody>(b => b.Id == 10 && b.AmendBody == "We  will proceed."), 7), Times.Once);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.Is<AmendmentBody>(b => b.Id == 20), 7), updateCurrentBodies ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task Characterization_AlreadyApproved_ResaveDoesNotRunCleanupOrParentPropagation()
    {
        _mockAmendmentService.Setup(x => x.GetAsync(2)).ReturnsAsync(new Model.DataModel.Amendment { Id = 2, ParentAmendmentId = 1, IsApproved = true });
        SetupSuccessfulSaves();

        var result = await CreateHandlerWithRealCleanup().Handle(ApproveCommand(2, 1, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mockAmendmentBodyService.Verify(x => x.GetByAmentmentId(It.IsAny<int>()), Times.Never);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()), Times.Never);
        _mockAmendmentService.Verify(x => x.UpdateAsync(It.IsAny<Model.DataModel.Amendment>(), 7), Times.Once);
    }

    [Fact]
    public async Task Characterization_Unapprove_DoesNotTouchBodies()
    {
        _mockAmendmentService.Setup(x => x.GetAsync(2)).ReturnsAsync(new Model.DataModel.Amendment { Id = 2, ParentAmendmentId = 1, IsApproved = true });
        SetupSuccessfulSaves();
        var command = ApproveCommand(2, 1, true);
        command.IsApproved = false;

        var result = await CreateHandlerWithRealCleanup().Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mockAmendmentBodyService.Verify(x => x.GetByAmentmentId(It.IsAny<int>()), Times.Never);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Characterization_MultiLanguage_MatchesByLanguageId_SkipsUnmatchedAndUnchanged()
    {
        var parentEnglish = Body(11, 1, 1, "Old English");
        var parentSpanish = Body(12, 1, 2, "Texto  nuevo");
        _mockAmendmentService.Setup(x => x.GetAsync(2)).ReturnsAsync(new Model.DataModel.Amendment { Id = 2, ParentAmendmentId = 1, IsApproved = false });
        _mockAmendmentService.Setup(x => x.GetAsync(1)).ReturnsAsync(new Model.DataModel.Amendment { Id = 1, IsLive = true });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(2)).ReturnsAsync(new List<AmendmentBody>
        {
            Body(21, 2, 1, "New ~~Old~~ <u>English</u>"),
            Body(22, 2, 2, "Texto ~~viejo~~ <U>nuevo</U>"),
            Body(23, 2, 3, "Nouveau ~~texte~~")
        });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1)).ReturnsAsync(new List<AmendmentBody> { parentEnglish, parentSpanish });
        SetupSuccessfulSaves();

        var result = await CreateHandlerWithRealCleanup().Handle(ApproveCommand(2, 1, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New  English", parentEnglish.AmendBody);
        Assert.Equal("Texto  nuevo", parentSpanish.AmendBody);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.Is<AmendmentBody>(b => b.Id == 11), 7), Times.Once);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.Is<AmendmentBody>(b => b.Id == 12), It.IsAny<int>()), Times.Never);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.Is<AmendmentBody>(b => b.LanguageId == 3), It.IsAny<int>()), Times.Never);
        _mockScreenControlService.Verify(x => x.UpdateBodyAsync(It.Is<AmendmentBody>(b => b.Id == 11), true), Times.Once);
        _mockScreenControlService.Verify(x => x.UpdateBodyAsync(It.Is<AmendmentBody>(b => b.Id == 12), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Characterization_MissingParent_ApprovalStillSucceeds()
    {
        _mockAmendmentService.Setup(x => x.GetAsync(2)).ReturnsAsync(new Model.DataModel.Amendment { Id = 2, ParentAmendmentId = 99, IsApproved = false });
        _mockAmendmentService.Setup(x => x.GetAsync(99)).ReturnsAsync((Model.DataModel.Amendment?)null);
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(2)).ReturnsAsync(new List<AmendmentBody> { Body(20, 2, 1, "A ~~b~~ c") });
        SetupSuccessfulSaves();

        var result = await CreateHandlerWithRealCleanup().Handle(ApproveCommand(2, 99, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mockAmendmentBodyService.Verify(x => x.GetByAmentmentId(99), Times.Never);
        _mockAmendmentBodyService.Verify(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()), Times.Never);
        _mockAmendmentService.Verify(x => x.UpdateAsync(It.IsAny<Model.DataModel.Amendment>(), 7), Times.Once);
    }

    [Fact]
    public async Task Characterization_ParentUpdateThrows_ExceptionSwallowed_ApprovalStillSaved()
    {
        _mockAmendmentService.Setup(x => x.GetAsync(2)).ReturnsAsync(new Model.DataModel.Amendment { Id = 2, ParentAmendmentId = 1, IsApproved = false });
        _mockAmendmentService.Setup(x => x.GetAsync(1)).ReturnsAsync(new Model.DataModel.Amendment { Id = 1 });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(2)).ReturnsAsync(new List<AmendmentBody> { Body(20, 2, 1, "A ~~b~~ c") });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1)).ThrowsAsync(new InvalidOperationException("db down"));
        SetupSuccessfulSaves();

        var result = await CreateHandlerWithRealCleanup().Handle(ApproveCommand(2, 1, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _mockAmendmentService.Verify(x => x.UpdateAsync(It.Is<Model.DataModel.Amendment>(a => a.IsApproved), 7), Times.Once);
    }

    [Fact]
    public async Task Characterization_BodiesSavedBeforeAmendment_FailedAmendmentSaveLeavesPartialUpdate()
    {
        var calls = new List<string>();
        _mockAmendmentService.Setup(x => x.GetAsync(2)).ReturnsAsync(new Model.DataModel.Amendment { Id = 2, ParentAmendmentId = 1, IsApproved = false });
        _mockAmendmentService.Setup(x => x.GetAsync(1)).ReturnsAsync(new Model.DataModel.Amendment { Id = 1 });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(2)).ReturnsAsync(new List<AmendmentBody> { Body(20, 2, 1, "A ~~b~~ c") });
        _mockAmendmentBodyService.Setup(x => x.GetByAmentmentId(1)).ReturnsAsync(new List<AmendmentBody> { Body(10, 1, 1, "old") });
        _mockAmendmentBodyService.Setup(x => x.UpdateAsync(It.IsAny<AmendmentBody>(), It.IsAny<int>()))
            .Callback<AmendmentBody, int>((b, _) => calls.Add($"body:{b.Id}"))
            .ReturnsAsync(new OperationResult(OperationType.Update, true));
        _mockAmendmentService.Setup(x => x.UpdateAsync(It.IsAny<Model.DataModel.Amendment>(), It.IsAny<int>()))
            .Callback<Model.DataModel.Amendment, int>((a, _) => calls.Add($"amendment:{a.Id}"))
            .ReturnsAsync(new OperationResult(OperationType.Update, false, "save failed"));

        var result = await CreateHandlerWithRealCleanup().Handle(ApproveCommand(2, 1, true), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(new[] { "body:20", "body:10", "amendment:2" }, calls);
    }

}
