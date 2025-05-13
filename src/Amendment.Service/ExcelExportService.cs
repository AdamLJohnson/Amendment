using Amendment.Model.DataModel;
using Amendment.Service.Infrastructure;
using ClosedXML.Excel;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Amendment.Service
{
    public class ExcelExportService : IExcelExportService
    {
        private const string SLIDE_DELIMITER = "**NEWSLIDE**";
        private readonly IMarkdownFormattingService _markdownService;

        public ExcelExportService(IMarkdownFormattingService markdownService)
        {
            _markdownService = markdownService;
        }

        /// <summary>
        /// Exports amendments to Excel format with rich text formatting
        /// </summary>
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
                        var slideText = slides[i];
                        var cell = worksheet.Cell(row, 8 + i);
                        
                        // Convert markdown to HTML to preserve formatting
                        var html = _markdownService.ConvertMarkdownToHtml(slideText);
                        
                        // Apply rich text formatting
                        ApplyRichTextFormatting(cell, html);
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

        /// <summary>
        /// Applies rich text formatting to an Excel cell based on HTML content
        /// </summary>
        private void ApplyRichTextFormatting(IXLCell cell, string htmlContent)
        {
            // Parse the HTML content
            var htmlDoc = _markdownService.ParseHtml(htmlContent);
            
            // Create rich text for the cell
            var richText = cell.CreateRichText();
            
            // Process the HTML nodes and apply formatting
            ProcessHtmlNodeForRichText(richText, htmlDoc.DocumentNode);
        }

        /// <summary>
        /// Recursively processes HTML nodes to apply formatting to rich text
        /// </summary>
        private void ProcessHtmlNodeForRichText(IXLRichText richText, HtmlNode node)
        {
            foreach (var childNode in node.ChildNodes)
            {
                if (childNode.NodeType == HtmlNodeType.Text)
                {
                    // Get text content
                    var text = System.Net.WebUtility.HtmlDecode(childNode.InnerText);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Extract formatting from parent nodes
                        var formatting = _markdownService.ExtractFormattingInfo(childNode);
                        
                        // Add text with formatting
                        var rt = richText.AddText(text);
                        
                        // Apply formatting
                        if (formatting["Bold"])
                            rt.Bold = true;
                        
                        if (formatting["Italic"])
                            rt.Italic = true;
                        
                        if (formatting["Underline"])
                            rt.Underline = XLFontUnderlineValues.Single;
                        
                        if (formatting["Strikethrough"])
                            rt.Strikethrough = true;
                        
                        if (formatting["Code"])
                            rt.FontName = "Courier New";
                    }
                }
                else
                {
                    // Process child nodes recursively
                    ProcessHtmlNodeForRichText(richText, childNode);
                }
            }
        }
    }
}
