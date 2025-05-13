using Amendment.Service.Infrastructure;
using HtmlAgilityPack;
using Markdig;
using System.Collections.Generic;

namespace Amendment.Service
{
    public class MarkdownFormattingService : IMarkdownFormattingService
    {
        /// <summary>
        /// Converts markdown to HTML
        /// </summary>
        public string ConvertMarkdownToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseSoftlineBreakAsHardlineBreak()
                .Build();

            return Markdown.ToHtml(markdown.Trim(), pipeline);
        }

        /// <summary>
        /// Converts markdown to plain text
        /// </summary>
        public string ConvertMarkdownToPlainText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseSoftlineBreakAsHardlineBreak()
                .Build();

            return Markdown.ToPlainText(markdown.Trim(), pipeline);
        }

        /// <summary>
        /// Parses HTML content into an HtmlDocument
        /// </summary>
        public HtmlDocument ParseHtml(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            return htmlDoc;
        }

        /// <summary>
        /// Extracts formatting information from an HTML node
        /// </summary>
        public Dictionary<string, bool> ExtractFormattingInfo(HtmlNode node)
        {
            var formatting = new Dictionary<string, bool>
            {
                { "Bold", false },
                { "Italic", false },
                { "Underline", false },
                { "Strikethrough", false },
                { "Code", false }
            };

            // Check node name for formatting
            switch (node.Name.ToLower())
            {
                case "strong":
                case "b":
                    formatting["Bold"] = true;
                    break;
                case "em":
                case "i":
                    formatting["Italic"] = true;
                    break;
                case "u":
                    formatting["Underline"] = true;
                    break;
                case "del":
                case "s":
                case "strike":
                    formatting["Strikethrough"] = true;
                    break;
                case "code":
                case "pre":
                    formatting["Code"] = true;
                    break;
            }

            // Check parent nodes for nested formatting
            var parentNode = node.ParentNode;
            while (parentNode != null && parentNode.NodeType != HtmlNodeType.Document)
            {
                switch (parentNode.Name.ToLower())
                {
                    case "strong":
                    case "b":
                        formatting["Bold"] = true;
                        break;
                    case "em":
                    case "i":
                        formatting["Italic"] = true;
                        break;
                    case "u":
                        formatting["Underline"] = true;
                        break;
                    case "del":
                    case "s":
                    case "strike":
                        formatting["Strikethrough"] = true;
                        break;
                    case "code":
                    case "pre":
                        formatting["Code"] = true;
                        break;
                }
                parentNode = parentNode.ParentNode;
            }

            return formatting;
        }
    }
}
