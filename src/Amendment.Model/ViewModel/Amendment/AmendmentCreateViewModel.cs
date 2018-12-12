using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Amendment.Model.DataModel;

namespace Amendment.Model.ViewModel.Amendment
{
    public class AmendmentCreateViewModel
    {
        [Required]
        [Display(Name="Amendment Title")]
        public string AmendTitle { get; set; }
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        [Display(Name = "Legislative ID")]
        public string LegisId { get; set; }
        [Required]
        [Display(Name = "Primary Language")]
        public int PrimaryLanguageId { get; set; } = 1;
        [Required]
        [Display(Name = "Amendment Body")]
        public string AmendBody { get; set; }

        public List<Language> Languages { get; set; }
    }
}
