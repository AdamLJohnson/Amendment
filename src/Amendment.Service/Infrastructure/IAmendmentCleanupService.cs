namespace Amendment.Service.Infrastructure
{
    /// <summary>
    /// Service interface for cleaning up amendment text by removing strikethrough and underline formatting
    /// </summary>
    public interface IAmendmentCleanupService
    {
        /// <summary>
        /// Cleans up amendment text by removing strikethrough and underline formatting
        /// </summary>
        /// <param name="amendmentText">The amendment text to clean up</param>
        /// <returns>Cleaned amendment text with formatting removed</returns>
        string CleanupAmendmentText(string amendmentText);

        /// <summary>
        /// Removes strikethrough formatting from text
        /// </summary>
        /// <param name="text">Text containing strikethrough formatting</param>
        /// <returns>Text with strikethrough formatting removed</returns>
        string RemoveStrikethrough(string text);

        /// <summary>
        /// Removes underline formatting from text
        /// </summary>
        /// <param name="text">Text containing underline formatting</param>
        /// <returns>Text with underline formatting removed</returns>
        string RemoveUnderline(string text);

        /// <summary>
        /// Checks if text contains amendment formatting (strikethrough or underline)
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>True if text contains amendment formatting, false otherwise</returns>
        bool ContainsAmendmentFormatting(string text);
    }
}
