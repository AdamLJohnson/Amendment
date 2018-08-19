using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Model.Enums;

namespace Amendment.Model.ViewModel.AmendmentBody
{
    public class AmendmentBodyDetailsViewModel
    {
        public int Id { get; set; }
        public int LanguageId { get; set; }
        public int AmendId { get; set; }
        public Language Language { get; set; }
        public string AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
    }
}
