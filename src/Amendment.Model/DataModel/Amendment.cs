﻿using System;
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
        [Column(TypeName = "longtext character set utf16")]
        public string AmendTitle { get; set; }
        [Column(TypeName = "longtext character set utf16")]
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        public string LegisId { get; set; }
        public bool IsLive { get; set; }

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


        public static Amendment GenerateNew(int iteration)
        {
            return new Model.DataModel.Amendment()
            {
                AmendTitle = $"Test Amendment {iteration}",
                Author = LoremNET.Lorem.Words(2),
                Motion = $"WC-{iteration}",
                LegisId = $"Legid {iteration}",
                PrimaryLanguageId = 1,
                Source = "Conference Floor",
                EnteredBy = -1,
                EnteredDate = DateTime.UtcNow,
                LastUpdatedBy = -1,
                LastUpdated = DateTime.UtcNow,
                AmendmentBodies = new List<AmendmentBody>()
                {
                    new AmendmentBody() {AmendBody = LoremNET.Lorem.Paragraph(6, 5), LanguageId = 1, IsLive = false},
                    new AmendmentBody() {AmendBody = LoremNET.Lorem.Paragraph(6, 5), LanguageId = 2, IsLive = false},
                    new AmendmentBody() {AmendBody = LoremNET.Lorem.Paragraph(6, 5), LanguageId = 3, IsLive = false}
                },
                IsLive = false
            };
        }
    }
}
