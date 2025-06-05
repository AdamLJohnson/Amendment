using Amendment.Service;
using Amendment.Service.Infrastructure;
using Xunit;

namespace Amendment.Server.Tests
{
    public class AmendmentCleanupServiceTests
    {
        private readonly IAmendmentCleanupService _cleanupService;

        public AmendmentCleanupServiceTests()
        {
            _cleanupService = new AmendmentCleanupService();
        }

        [Fact]
        public void RemoveStrikethrough_WithSimpleStrikethrough_RemovesText()
        {
            // Arrange
            var input = "This is ~~removed text~~ and this remains.";
            var expected = "This is  and this remains.";

            // Act
            var result = _cleanupService.RemoveStrikethrough(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RemoveStrikethrough_WithMultipleStrikethroughs_RemovesAllText()
        {
            // Arrange
            var input = "Keep this ~~remove this~~ keep this ~~remove this too~~ keep this.";
            var expected = "Keep this  keep this  keep this.";

            // Act
            var result = _cleanupService.RemoveStrikethrough(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RemoveStrikethrough_WithNoStrikethrough_ReturnsOriginal()
        {
            // Arrange
            var input = "This text has no strikethrough formatting.";

            // Act
            var result = _cleanupService.RemoveStrikethrough(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void RemoveUnderline_WithSimpleUnderline_KeepsTextRemovesFormatting()
        {
            // Arrange
            var input = "This is <u>underlined text</u> and this is normal.";
            var expected = "This is underlined text and this is normal.";

            // Act
            var result = _cleanupService.RemoveUnderline(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RemoveUnderline_WithMultipleUnderlines_KeepsAllTextRemovesFormatting()
        {
            // Arrange
            var input = "Keep this <u>underlined</u> and this <u>also underlined</u> text.";
            var expected = "Keep this underlined and this also underlined text.";

            // Act
            var result = _cleanupService.RemoveUnderline(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void RemoveUnderline_WithNoUnderline_ReturnsOriginal()
        {
            // Arrange
            var input = "This text has no underline formatting.";

            // Act
            var result = _cleanupService.RemoveUnderline(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void CleanupAmendmentText_WithBothFormattings_RemovesStrikethroughKeepsUnderlineText()
        {
            // Arrange
            var input = "Keep this <u>new text</u> but remove ~~old text~~ completely.";
            var expected = "Keep this new text but remove  completely.";

            // Act
            var result = _cleanupService.CleanupAmendmentText(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CleanupAmendmentText_WithComplexExample_CleansUpCorrectly()
        {
            // Arrange
            var input = @"**Motion WC-123**

The committee recommends that ~~the old policy~~ <u>the new policy</u> be adopted.

Furthermore, ~~section 2.1~~ <u>section 2.2</u> should be ~~removed~~ <u>updated</u> to reflect current practices.

~~This entire paragraph should be removed as it's outdated.~~

<u>This new paragraph contains the updated information that should remain.</u>";

            var expected = @"**Motion WC-123**

The committee recommends that  the new policy be adopted.

Furthermore,  section 2.2 should be  updated to reflect current practices.



This new paragraph contains the updated information that should remain.";

            // Act
            var result = _cleanupService.CleanupAmendmentText(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ContainsAmendmentFormatting_WithStrikethrough_ReturnsTrue()
        {
            // Arrange
            var input = "This has ~~strikethrough~~ text.";

            // Act
            var result = _cleanupService.ContainsAmendmentFormatting(input);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsAmendmentFormatting_WithUnderline_ReturnsTrue()
        {
            // Arrange
            var input = "This has <u>underlined</u> text.";

            // Act
            var result = _cleanupService.ContainsAmendmentFormatting(input);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsAmendmentFormatting_WithBothFormattings_ReturnsTrue()
        {
            // Arrange
            var input = "This has <u>underlined</u> and ~~strikethrough~~ text.";

            // Act
            var result = _cleanupService.ContainsAmendmentFormatting(input);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ContainsAmendmentFormatting_WithNoFormatting_ReturnsFalse()
        {
            // Arrange
            var input = "This has no special formatting.";

            // Act
            var result = _cleanupService.ContainsAmendmentFormatting(input);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CleanupAmendmentText_WithEmptyString_ReturnsEmpty()
        {
            // Arrange
            var input = "";

            // Act
            var result = _cleanupService.CleanupAmendmentText(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void CleanupAmendmentText_WithNull_ReturnsEmpty()
        {
            // Arrange
            string input = null;

            // Act
            var result = _cleanupService.CleanupAmendmentText(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void RemoveUnderline_CaseInsensitive_RemovesUppercaseTags()
        {
            // Arrange
            var input = "This has <U>uppercase underline</U> tags.";
            var expected = "This has uppercase underline tags.";

            // Act
            var result = _cleanupService.RemoveUnderline(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CleanupAmendmentText_WithNestedFormatting_HandlesCorrectly()
        {
            // Arrange
            var input = "Text with <u>underlined ~~and strikethrough~~</u> content.";
            var expected = "Text with underlined  content.";

            // Act
            var result = _cleanupService.CleanupAmendmentText(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CleanupAmendmentText_RealWorldExample_CleansUpCorrectly()
        {
            // Arrange - Real world example of amendment text
            var input = @"**Motion 2024-001**

WHEREAS the committee has reviewed the current policy ~~regarding remote work arrangements~~ <u>regarding flexible work arrangements</u>;

WHEREAS the ~~old guidelines~~ <u>updated guidelines</u> better reflect current needs;

~~WHEREAS the previous section about office requirements is no longer relevant;~~

<u>WHEREAS the new section about hybrid work models provides better clarity;</u>

THEREFORE BE IT RESOLVED that ~~Policy 123~~ <u>Policy 456</u> be adopted ~~immediately~~ <u>effective January 1, 2025</u>.";

            var expected = @"**Motion 2024-001**

WHEREAS the committee has reviewed the current policy  regarding flexible work arrangements;

WHEREAS the  updated guidelines better reflect current needs;



WHEREAS the new section about hybrid work models provides better clarity;

THEREFORE BE IT RESOLVED that  Policy 456 be adopted  effective January 1, 2025.";

            // Act
            var result = _cleanupService.CleanupAmendmentText(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
