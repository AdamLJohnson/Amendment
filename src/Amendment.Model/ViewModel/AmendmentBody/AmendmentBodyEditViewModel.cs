using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Model.Enums;

namespace Amendment.Model.ViewModel.AmendmentBody
{
    public class AmendmentBodyEditViewModel
    {
        [Required]
        public int AmendId { get; set; }
        [Required]
        [Display(Name = "Language")]
        public int? LanguageId { get; set; }
        [Required]
        [Display(Name = "Amendment Body")]
        public string AmendBody { get; set; }
        [Display(Name = "Status")]
        public AmendmentBodyStatus AmendStatus { get; set; }
        public List<Language> Languages { get; set; }
    }
}
