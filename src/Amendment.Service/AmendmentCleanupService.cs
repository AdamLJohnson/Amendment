using Amendment.Service.Infrastructure;
using System.Text.RegularExpressions;

namespace Amendment.Service
{
    /// <summary>
    /// Service for cleaning up amendment text by removing strikethrough and underline formatting
    /// </summary>
    public class AmendmentCleanupService : IAmendmentCleanupService
    {
        /// <summary>
        /// Cleans up amendment text by removing strikethrough and underline formatting
        /// </summary>
        /// <param name="amendmentText">The amendment text to clean up</param>
        /// <returns>Cleaned amendment text with formatting removed</returns>
        public string CleanupAmendmentText(string amendmentText)
        {
            if (string.IsNullOrEmpty(amendmentText))
                return string.Empty;

            var cleanedText = amendmentText;

            // Remove strikethrough formatting (~~text~~)
            cleanedText = RemoveStrikethrough(cleanedText);

            // Remove underline formatting (<u>text</u>)
            cleanedText = RemoveUnderline(cleanedText);

            return cleanedText;
        }

        /// <summary>
        /// Removes strikethrough formatting from text
        /// </summary>
        /// <param name="text">Text containing strikethrough formatting</param>
        /// <returns>Text with strikethrough formatting removed</returns>
        public string RemoveStrikethrough(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Remove markdown strikethrough (~~text~~)
            // This regex matches ~~ followed by any content (non-greedy) followed by ~~
            var strikethroughPattern = @"~~(.*?)~~";
            return Regex.Replace(text, strikethroughPattern, "", RegexOptions.Singleline);
        }

        /// <summary>
        /// Removes underline formatting from text
        /// </summary>
        /// <param name="text">Text containing underline formatting</param>
        /// <returns>Text with underline formatting removed</returns>
        public string RemoveUnderline(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Remove HTML underline tags (<u>text</u>)
            // This regex matches <u> followed by any content (non-greedy) followed by </u>
            var underlinePattern = @"<u>(.*?)</u>";
            return Regex.Replace(text, underlinePattern, "$1", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Checks if text contains amendment formatting (strikethrough or underline)
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>True if text contains amendment formatting, false otherwise</returns>
        public bool ContainsAmendmentFormatting(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Check for strikethrough formatting
            var strikethroughPattern = @"~~.*?~~";
            if (Regex.IsMatch(text, strikethroughPattern, RegexOptions.Singleline))
                return true;

            // Check for underline formatting
            var underlinePattern = @"<u>.*?</u>";
            if (Regex.IsMatch(text, underlinePattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
                return true;

            return false;
        }
    }
}
