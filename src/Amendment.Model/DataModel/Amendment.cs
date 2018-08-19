using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Amendment.Model.Infrastructure;

namespace Amendment.Model.DataModel
{
    public class Amendment : ITableBase
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string AmendTitle { get; set; }
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        public string LegisId { get; set; }

        [Required]
        public int PrimaryLanguageId { get; set; }
        [Required]
        public Language PrimaryLanguage { get; set; }
        [ForeignKey("AmendId")]
        public List<AmendmentBody> AmendmentBodies { get; set; } = new List<AmendmentBody>();

        public int EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
