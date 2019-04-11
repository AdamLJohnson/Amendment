using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Amendment.Model.Enums;
using Amendment.Model.Infrastructure;
using Markdig;

namespace Amendment.Model.DataModel
{
    public class AmendmentBody : ITableBase
    {
        private const string PageSplit = "**NEWSLIDE**";

        [Key]
        public int Id { get; set; }
        [Required]
        public int AmendId { get; set; }
        [Required]
        public int LanguageId { get; set; }
        [Required]
        public string AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
        public bool IsLive { get; set; }

        public Language Language { get; set; }


        public int EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public int Page { get; set; } = 0;

        [NotMapped]
        public int Pages
        {
            get
            {
                if (string.IsNullOrEmpty(AmendBody))
                    return 0;

                return AmendBody.Split(PageSplit).Length;
            }
        }

        public string AmendBodyHtml
        {
            get
            {
                if (string.IsNullOrEmpty(AmendBody))
                    return "";
                return renderHtml(AmendBody);
            }
        }

        public string AmendBodyPagedHtml
        {
            get
            {
                if (string.IsNullOrEmpty(AmendBody))
                    return "";

                var pages = AmendBody.Split(PageSplit);
                if (Page > Pages)
                    return renderHtml(pages[0]);

                if (Page < pages.Length)
                    return renderHtml(pages[Page]);

                return "";
            }
        }

        public string[] AmendBodyPages
        {
            get
            {
                if (string.IsNullOrEmpty(AmendBody))
                    return new string[0];
                var output = new List<string>();

                foreach (var p in AmendBody.Split(PageSplit))
                    output.Add(renderHtml(p));

                return output.ToArray();
            }
        }

        private string renderHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak().Build();
            return Markdown.ToHtml(text, pipeline);
        }
    }
}