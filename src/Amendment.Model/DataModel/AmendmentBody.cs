using System;
using System.ComponentModel.DataAnnotations;
using Amendment.Model.Enums;
using Amendment.Model.Infrastructure;
using Markdig;

namespace Amendment.Model.DataModel
{
    public class AmendmentBody : ITableBase
    {
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

        public string AmendBodyHtml
        {
            get
            {
                if (string.IsNullOrEmpty(AmendBody))
                    return "";
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSoftlineBreakAsHardlineBreak().Build();
                return Markdown.ToHtml(AmendBody, pipeline);
            }
        }
    }
}