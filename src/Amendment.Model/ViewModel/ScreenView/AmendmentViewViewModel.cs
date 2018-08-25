using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.DataModel;

namespace Amendment.Model.ViewModel.ScreenView
{
    public class AmendmentViewViewModel
    {
        public int Id { get; set; }
        public string AmendTitle { get; set; }
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        public string LegisId { get; set; }
        public bool IsLive { get; set; }

        public int PrimaryLanguageId { get; set; }
        public Language PrimaryLanguage { get; set; } = new Language();
    }
}
