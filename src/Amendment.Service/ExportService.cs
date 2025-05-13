﻿using Amendment.Model.DataModel;
using Amendment.Service.Infrastructure;
using ClosedXML.Excel;
using HtmlAgilityPack;
using Markdig;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amendment.Service
{
    public class ExportService : IExportService
    {
        private const string SLIDE_DELIMITER = "**NEWSLIDE**";

        // Language IDs based on the database seed data
        private const int ENGLISH_LANGUAGE_ID = 1;
        private const int SPANISH_LANGUAGE_ID = 2;
        private const int FRENCH_LANGUAGE_ID = 3;

        public async Task<MemoryStream> ExportAmendmentsToExcelAsync(IEnumerable<Model.DataModel.Amendment> amendments)
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Amendments");

                // Add headers
                worksheet.Cell(1, 1).Value = "Amendment Title";
                worksheet.Cell(1, 2).Value = "Author";
                worksheet.Cell(1, 3).Value = "Motion";
                worksheet.Cell(1, 4).Value = "Source";
                worksheet.Cell(1, 5).Value = "Legislative ID";
                worksheet.Cell(1, 6).Value = "Is Archived";
                worksheet.Cell(1, 7).Value = "Language";

                // Format header row
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Find the maximum number of slides across all amendments
                int maxSlides = 0;
                foreach (var amendment in amendments)
                {
                    foreach (var body in amendment.AmendmentBodies)
                    {
                        var slides = body.AmendBody.Split(SLIDE_DELIMITER, StringSplitOptions.RemoveEmptyEntries);
                        maxSlides = Math.Max(maxSlides, slides.Length);
                    }
                }

                // Add slide headers
                for (int i = 0; i < maxSlides; i++)
                {
                    worksheet.Cell(1, 8 + i).Value = $"Slide {i + 1}";
                    worksheet.Cell(1, 8 + i).Style.Font.Bold = true;
                    worksheet.Cell(1, 8 + i).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Add data rows
                int row = 2;
                foreach (var amendment in amendments)
                foreach (var body in amendment.AmendmentBodies)
                {
                    worksheet.Cell(row, 1).Value = amendment.Title;
                    worksheet.Cell(row, 2).Value = amendment.Author;
                    worksheet.Cell(row, 3).Value = amendment.Motion;
                    worksheet.Cell(row, 4).Value = amendment.Source;
                    worksheet.Cell(row, 5).Value = amendment.LegisId;
                    worksheet.Cell(row, 6).Value = amendment.IsArchived ? "Yes" : "No";
                    worksheet.Cell(row, 7).Value = body.Language.LanguageName;
                    var slides = body.AmendBody.Split(SLIDE_DELIMITER, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < slides.Length; i++)
                    {
                        var text = slides[i];
                        var cell = worksheet.Cell(row, 8 + i);
                        text = Markdown.ToPlainText(text, new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak().Build());
                        cell.Value = text.Trim();
                    }
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                worksheet.Rows(2, row - 1).Height = 100;
                worksheet.Rows(2, row - 1).Style.Alignment.WrapText = true;
                worksheet.Rows(2, row - 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                // Set reasonable max width for content columns
                for (int i = 1; i <= 7 + maxSlides; i++)
                {
                    if (worksheet.Column(i).Width > 100)
                    {
                        worksheet.Column(i).Width = 100;
                    }
                }

                // Save to MemoryStream
                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            });
        }

        public async Task<MemoryStream> ExportAmendmentsToPdfAsync(IEnumerable<Model.DataModel.Amendment> amendments)
        {
            // Configure QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            return await Task.Run(() =>
            {
                var stream = new MemoryStream();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Set page settings
                        page.Size(PageSizes.Letter.Landscape());
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                        // Add header
                        page.Header().Element(ComposeHeader);

                        // Add content
                        page.Content().Element(container => ComposeContent(container, amendments));

                        // Add footer with page numbers and timestamp
                        page.Footer().Row(row =>
                        {
                            // Left side - timestamp
                            row.RelativeItem().AlignLeft().Text(text =>
                            {
                                text.Span($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Medium);
                            });

                            // Right side - page numbers
                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Page ").FontSize(9);
                                text.CurrentPageNumber().FontSize(9);
                                text.Span(" of ").FontSize(9);
                                text.TotalPages().FontSize(9);
                            });
                        });
                    });
                })
                .GeneratePdf(stream);

                stream.Position = 0;
                return stream;
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(5).PaddingBottom(10).Row(row =>
            {
                // Logo/Title section
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Amendment Export")
                        .FontSize(22)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken1);

                    column.Item().Text("Official Document")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                // Right-aligned organization info
                row.RelativeItem().AlignRight().Column(column =>
                {
                    column.Item().Text("Amendment Management System")
                        .FontSize(12)
                        .SemiBold();

                    // column.Item().Text("Confidential Document")
                    //     .FontSize(9)
                    //     .FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private void ComposeContent(IContainer container, IEnumerable<Model.DataModel.Amendment> amendments)
        {
            container.Column(column =>
            {
                bool isFirst = true;

                foreach (var amendment in amendments)
                {
                    // Add page break between amendments (except for the first one)
                    if (!isFirst)
                    {
                        column.Item().PageBreak();
                    }
                    isFirst = false;

                    // Amendment metadata section
                    column.Item().Element(container => ComposeAmendmentMetadata(container, amendment));

                    // Amendment body section with three columns
                    column.Item().Element(container => ComposeAmendmentBody(container, amendment));
                }
            });
        }

        private void ComposeAmendmentMetadata(IContainer container, Model.DataModel.Amendment amendment)
        {
            container.Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(column =>
            {
                // Amendment title at the top
                column.Item().Text($"Amendment: {amendment.Title}")
                    .FontSize(16)
                    .SemiBold();

                // Metadata in a three-column layout
                column.Item().PaddingTop(5).Row(row =>
                {
                    // First column
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Author: ").SemiBold();
                            text.Span(amendment.Author ?? "");
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Motion: ").SemiBold();
                            text.Span(amendment.Motion ?? "");
                        });
                    });

                    // Second column
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Source: ").SemiBold();
                            text.Span(amendment.Source ?? "");
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Legislative ID: ").SemiBold();
                            text.Span(amendment.LegisId ?? "");
                        });
                    });

                    // Third column
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Status: ").SemiBold();
                            text.Span(amendment.IsArchived ? "Archived" : "Active");
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Primary Language: ").SemiBold();
                            text.Span(amendment.PrimaryLanguage?.LanguageName ?? "");
                        });
                    });
                });
            });
        }

        private void ComposeAmendmentBody(IContainer container, Model.DataModel.Amendment amendment)
        {
            // Get the bodies for each language
            var englishBody = amendment.AmendmentBodies.FirstOrDefault(b => b.LanguageId == ENGLISH_LANGUAGE_ID);
            var spanishBody = amendment.AmendmentBodies.FirstOrDefault(b => b.LanguageId == SPANISH_LANGUAGE_ID);
            var frenchBody = amendment.AmendmentBodies.FirstOrDefault(b => b.LanguageId == FRENCH_LANGUAGE_ID);

            container.PaddingTop(10).Column(column =>
            {
                // Section header with background
                column.Item().Background(Colors.Blue.Lighten5).BorderTop(1).BorderBottom(1)
                    .BorderColor(Colors.Blue.Lighten3).Padding(5)
                    .Text("Amendment Body").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken1);

                // Three-column layout for language content
                column.Item().PaddingTop(8).Row(row =>
                {
                    // English column
                    row.RelativeItem().Column(column =>
                    {
                        // Header with background
                        column.Item().Background(Colors.Grey.Lighten4)
                            .Border(1).BorderColor(Colors.Grey.Medium)
                            .Padding(5).AlignCenter()
                            .Text("English").SemiBold().FontSize(12);

                        // Content with border
                        column.Item().Border(1).BorderTop(0).BorderColor(Colors.Grey.Medium).Padding(8).MinHeight(200).Element(container =>
                        {
                            if (englishBody != null)
                            {
                                RenderMarkdownContent(container, englishBody.AmendBody);
                            }
                            else
                            {
                                container.Text("No English content available")
                                    .FontColor(Colors.Grey.Medium)
                                    .Italic();
                            }
                        });
                    });

                    // Small gap between columns
                    row.ConstantItem(10);

                    // Spanish column
                    row.RelativeItem().Column(column =>
                    {
                        // Header with background
                        column.Item().Background(Colors.Grey.Lighten4)
                            .Border(1).BorderColor(Colors.Grey.Medium)
                            .Padding(5).AlignCenter()
                            .Text("Spanish").SemiBold().FontSize(12);

                        // Content with border
                        column.Item().Border(1).BorderTop(0).BorderColor(Colors.Grey.Medium).Padding(8).MinHeight(200).Element(container =>
                        {
                            if (spanishBody != null)
                            {
                                RenderMarkdownContent(container, spanishBody.AmendBody);
                            }
                            else
                            {
                                container.Text("No Spanish content available")
                                    .FontColor(Colors.Grey.Medium)
                                    .Italic();
                            }
                        });
                    });

                    // Small gap between columns
                    row.ConstantItem(10);

                    // French column
                    row.RelativeItem().Column(column =>
                    {
                        // Header with background
                        column.Item().Background(Colors.Grey.Lighten4)
                            .Border(1).BorderColor(Colors.Grey.Medium)
                            .Padding(5).AlignCenter()
                            .Text("French").SemiBold().FontSize(12);

                        // Content with border
                        column.Item().Border(1).BorderTop(0).BorderColor(Colors.Grey.Medium).Padding(8).MinHeight(200).Element(container =>
                        {
                            if (frenchBody != null)
                            {
                                RenderMarkdownContent(container, frenchBody.AmendBody);
                            }
                            else
                            {
                                container.Text("No French content available")
                                    .FontColor(Colors.Grey.Medium)
                                    .Italic();
                            }
                        });
                    });
                });
            });
        }

        private void RenderMarkdownContent(IContainer container, string markdownContent)
        {
            // Split the content by slides
            var slides = markdownContent.Split(SLIDE_DELIMITER, StringSplitOptions.RemoveEmptyEntries);

            container.Column(column =>
            {
                for (int i = 0; i < slides.Length; i++)
                {
                    // For multiple slides, add slide headers and dividers
                    if (slides.Length > 1)
                    {
                        if (i > 0)
                        {
                            // Add a divider between slides
                            column.Item().PaddingTop(10).PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        }

                        // Add slide header with background
                        column.Item().Background(Colors.Grey.Lighten5)
                            .Border(1).BorderColor(Colors.Grey.Lighten3)
                            .Padding(3).PaddingLeft(8)
                            .Text($"Slide {i + 1} of {slides.Length}")
                            .FontSize(9).Italic().FontColor(Colors.Grey.Darken2);
                    }

                    // Convert markdown to HTML for rendering
                    var pipeline = new MarkdownPipelineBuilder()
                        .UseAdvancedExtensions()
                        .UseSoftlineBreakAsHardlineBreak()
                        .Build();

                    var html = Markdown.ToHtml(slides[i].Trim(), pipeline);

                    // Render the HTML content
                    column.Item().PaddingTop(3).Element(container => RenderHtmlContent(container, html));
                }
            });
        }

        private void RenderHtmlContent(IContainer container, string htmlContent)
        {
            // Use HtmlAgilityPack to parse the HTML content
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Process the HTML document
            container.Column(column =>
            {
                ProcessHtmlNode(column, htmlDoc.DocumentNode);
            });
        }

        private void ProcessHtmlNode(ColumnDescriptor column, HtmlNode node)
        {
            // Process each child node
            foreach (var childNode in node.ChildNodes)
            {
                switch (childNode.Name.ToLower())
                {
                    case "#text":
                        if (!string.IsNullOrWhiteSpace(childNode.InnerText))
                        {
                            // Decode HTML entities
                            var text = System.Net.WebUtility.HtmlDecode(childNode.InnerText.Trim());
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                column.Item().Text(text);
                            }
                        }
                        break;

                    case "p":
                        column.Item().Padding(3).Element(container =>
                        {
                            container.Column(innerColumn =>
                            {
                                // Use the new style tracking approach for paragraphs
                                innerColumn.Item().Text(text =>
                                {
                                    // Process each child node with style tracking
                                    foreach (var nestedNode in childNode.ChildNodes)
                                    {
                                        // Create a style tracker for this node
                                        var styleTracker = new StyleTracker();

                                        // Process the node with style tracking
                                        ProcessNodeWithStyleTracking(text, nestedNode, styleTracker);
                                    }
                                });
                            });
                        });
                        break;

                    case "h1":
                    case "h2":
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                        float fontSize = 16;
                        switch (childNode.Name.ToLower())
                        {
                            case "h1": fontSize = 20; break;
                            case "h2": fontSize = 18; break;
                            case "h3": fontSize = 16; break;
                            case "h4": fontSize = 14; break;
                            case "h5": fontSize = 12; break;
                            case "h6": fontSize = 11; break;
                        }

                        column.Item().PaddingTop(5).PaddingBottom(2).Text(text =>
                        {
                            // Create a style tracker for the header
                            var styleTracker = new StyleTracker { IsBold = true, FontSize = fontSize }; // Headers are bold by default

                            if (childNode.HasChildNodes)
                            {
                                // Process each child node with style tracking
                                foreach (var nestedNode in childNode.ChildNodes)
                                {
                                    // Clone the style tracker to avoid affecting siblings
                                    var nestedStyleTracker = styleTracker.Clone();

                                    // Process the nested node with style tracking
                                    ProcessNodeWithStyleTracking(text, nestedNode, nestedStyleTracker);
                                }
                            }
                            else
                            {
                                // Simple header with no nested elements
                                var span = text.Span(System.Net.WebUtility.HtmlDecode(childNode.InnerText))
                                    .FontSize(fontSize);
                                styleTracker.ApplyStyles(span);
                            }
                        });
                        break;

                    case "ul":
                        column.Item().Padding(3).Element(container =>
                        {
                            container.Column(innerColumn =>
                            {
                                ProcessUnorderedList(innerColumn, childNode);
                            });
                        });
                        break;

                    case "ol":
                        column.Item().Padding(3).Element(container =>
                        {
                            container.Column(innerColumn =>
                            {
                                ProcessOrderedList(innerColumn, childNode);
                            });
                        });
                        break;

                    case "blockquote":
                        column.Item().BorderLeft(2).BorderColor(Colors.Grey.Medium).PaddingLeft(10).Padding(3).Element(container =>
                        {
                            container.Column(innerColumn =>
                            {
                                ProcessHtmlNode(innerColumn, childNode);
                            });
                        });
                        break;

                    case "hr":
                        column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        break;

                    case "br":
                        column.Item().Height(10);
                        break;

                    default:
                        // For other elements, process their children
                        ProcessHtmlNode(column, childNode);
                        break;
                }
            }
        }

        private void ProcessHtmlNodeWithStyles(ColumnDescriptor column, HtmlNode node)
        {
            // Check if the node has any text content (direct or in children)
            if (string.IsNullOrWhiteSpace(GetTextContent(node)))
                return;

            column.Item().Text(text =>
            {
                ProcessTextWithStyles(text, node);
            });
        }

        private void ProcessTextWithStyles(TextDescriptor text, HtmlNode node)
        {
            // Process each child node with style tracking
            foreach (var childNode in node.ChildNodes)
            {
                // Create a style tracker for this node
                var styleTracker = new StyleTracker();

                // Process the node with style tracking
                ProcessNodeWithStyleTracking(text, childNode, styleTracker);
            }
        }

        private class StyleTracker
        {
            public bool IsBold { get; set; }
            public bool IsItalic { get; set; }
            public bool IsUnderline { get; set; }
            public bool IsStrikethrough { get; set; }
            public bool IsLink { get; set; }
            public bool IsCode { get; set; }
            public bool IsHeader { get; set; }
            public float? FontSize { get; set; }

            public StyleTracker Clone()
            {
                return new StyleTracker
                {
                    IsBold = this.IsBold,
                    IsItalic = this.IsItalic,
                    IsUnderline = this.IsUnderline,
                    IsStrikethrough = this.IsStrikethrough,
                    IsLink = this.IsLink,
                    IsCode = this.IsCode,
                    IsHeader = this.IsHeader,
                    FontSize = this.FontSize
                };
            }

            public TextSpanDescriptor ApplyStyles(TextSpanDescriptor span)
            {
                if (IsBold)
                    span = span.SemiBold();

                if (IsItalic)
                    span = span.Italic();

                if (IsUnderline)
                    span = span.Underline();

                if (IsStrikethrough)
                    span = span.Strikethrough();

                if (IsLink)
                    span = span.FontColor(Colors.Blue.Medium);

                if (IsCode)
                    span = span.FontFamily("Courier New").FontColor(Colors.Grey.Darken3);

                if (FontSize.HasValue)
                    span = span.FontSize(FontSize.Value);

                return span;
            }
        }

        private void ProcessNodeWithStyleTracking(TextDescriptor text, HtmlNode node, StyleTracker styleTracker)
        {
            // Update style tracker based on current node
            UpdateStyleTracker(node, styleTracker);

            if (node.NodeType == HtmlNodeType.Text)
            {
                // This is a text node, render it with current styles
                var content = System.Net.WebUtility.HtmlDecode(node.InnerText);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var span = text.Span(content);
                    styleTracker.ApplyStyles(span);
                }
            }
            else if (node.HasChildNodes)
            {
                // Process child nodes with current style context
                foreach (var childNode in node.ChildNodes)
                {
                    // Clone the style tracker to avoid affecting siblings
                    var childStyleTracker = styleTracker.Clone();
                    ProcessNodeWithStyleTracking(text, childNode, childStyleTracker);
                }
            }
            else if (!string.IsNullOrWhiteSpace(node.InnerText))
            {
                // Leaf node with text content
                var content = System.Net.WebUtility.HtmlDecode(node.InnerText);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var span = text.Span(content);
                    styleTracker.ApplyStyles(span);
                }
            }
        }

        private void UpdateStyleTracker(HtmlNode node, StyleTracker styleTracker)
        {
            switch (node.Name.ToLower())
            {
                case "strong":
                case "b":
                    styleTracker.IsBold = true;
                    break;

                case "em":
                case "i":
                    styleTracker.IsItalic = true;
                    break;

                case "u":
                    styleTracker.IsUnderline = true;
                    break;

                case "del":
                case "s":
                case "strike":
                    styleTracker.IsStrikethrough = true;
                    break;

                case "a":
                    styleTracker.IsLink = true;
                    // Links are also bold by default in our styling
                    styleTracker.IsBold = true;
                    break;

                case "code":
                case "pre":
                    styleTracker.IsCode = true;
                    break;

                case "h1":
                    styleTracker.IsHeader = true;
                    styleTracker.IsBold = true;
                    styleTracker.FontSize = 20;
                    break;

                case "h2":
                    styleTracker.IsHeader = true;
                    styleTracker.IsBold = true;
                    styleTracker.FontSize = 18;
                    break;

                case "h3":
                    styleTracker.IsHeader = true;
                    styleTracker.IsBold = true;
                    styleTracker.FontSize = 16;
                    break;

                case "h4":
                    styleTracker.IsHeader = true;
                    styleTracker.IsBold = true;
                    styleTracker.FontSize = 14;
                    break;

                case "h5":
                    styleTracker.IsHeader = true;
                    styleTracker.IsBold = true;
                    styleTracker.FontSize = 12;
                    break;

                case "h6":
                    styleTracker.IsHeader = true;
                    styleTracker.IsBold = true;
                    styleTracker.FontSize = 11;
                    break;
            }
        }

        private void ProcessUnorderedList(ColumnDescriptor column, HtmlNode listNode)
        {
            foreach (var item in listNode.Elements("li"))
            {
                column.Item().Text(text =>
                {
                    text.Span("• ").SemiBold();
                    if (item.HasChildNodes)
                    {
                        // Create a new style tracker for this list item
                        var styleTracker = new StyleTracker();

                        // Process each child node with style tracking
                        foreach (var childNode in item.ChildNodes)
                        {
                            ProcessNodeWithStyleTracking(text, childNode, styleTracker.Clone());
                        }
                    }
                    else
                    {
                        text.Span(System.Net.WebUtility.HtmlDecode(item.InnerText));
                    }
                });
            }
        }

        private void ProcessOrderedList(ColumnDescriptor column, HtmlNode listNode)
        {
            int index = 1;
            foreach (var item in listNode.Elements("li"))
            {
                column.Item().Text(text =>
                {
                    text.Span($"{index}. ").SemiBold();
                    if (item.HasChildNodes)
                    {
                        // Create a new style tracker for this list item
                        var styleTracker = new StyleTracker();

                        // Process each child node with style tracking
                        foreach (var childNode in item.ChildNodes)
                        {
                            ProcessNodeWithStyleTracking(text, childNode, styleTracker.Clone());
                        }
                    }
                    else
                    {
                        text.Span(System.Net.WebUtility.HtmlDecode(item.InnerText));
                    }
                });
                index++;
            }
        }

        private string GetTextContent(HtmlNode node)
        {
            return System.Net.WebUtility.HtmlDecode(Regex.Replace(node.InnerText, @"\s+", " ")).Trim();
        }


    }
}

