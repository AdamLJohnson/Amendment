﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Amendment.Model.DataModel;

namespace Amendment.Model.ViewModel.Amendment
{
    public class AmendmentEditViewModel
    {
        [Required]
        public string AmendTitle { get; set; }
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        public string LegisId { get; set; }
        [Required]
        public int PrimaryLanguageId { get; set; } = 1;

        public List<Language> Languages { get; set; }
        public List<DataModel.AmendmentBody> AmendmentBodies { get; set; }
    }
}