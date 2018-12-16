using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Model.Enums;

namespace Amendment.Model.ViewModel.ScreenView
{
    public class AmendmentBodyViewViewModel
    {
        public int Id { get; set; }
        public int AmendId { get; set; }
        public int LanguageId { get; set; }
        public string AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
        public bool IsLive { get; set; }

        public Language Language { get; set; } = new Language();
        public string AmendBodyHtml { get; set; }
        public string AmendBodyPagedHtml { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
    }
}
