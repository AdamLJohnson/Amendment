using HtmlAgilityPack;
using System.Collections.Generic;

namespace Amendment.Service.Infrastructure
{
    /// <summary>
    /// Service for processing and formatting markdown content
    /// </summary>
    public interface IMarkdownFormattingService
    {
        /// <summary>
        /// Converts markdown to HTML
        /// </summary>
        /// <param name="markdown">Markdown content</param>
        /// <returns>HTML content</returns>
        string ConvertMarkdownToHtml(string markdown);

        /// <summary>
        /// Converts markdown to plain text
        /// </summary>
        /// <param name="markdown">Markdown content</param>
        /// <returns>Plain text content</returns>
        string ConvertMarkdownToPlainText(string markdown);

        /// <summary>
        /// Parses HTML content into an HtmlDocument
        /// </summary>
        /// <param name="html">HTML content</param>
        /// <returns>Parsed HtmlDocument</returns>
        HtmlDocument ParseHtml(string html);

        /// <summary>
        /// Extracts formatting information from an HTML node
        /// </summary>
        /// <param name="node">HTML node</param>
        /// <returns>Dictionary of formatting attributes</returns>
        Dictionary<string, bool> ExtractFormattingInfo(HtmlNode node);
    }
}
