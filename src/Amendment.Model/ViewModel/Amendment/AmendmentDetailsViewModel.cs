using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Amendment.Model.DataModel;

namespace Amendment.Model.ViewModel.Amendment
{
    public class AmendmentDetailsViewModel
    {
        public int Id { get; set; }
        public string AmendTitle { get; set; }
        public string Author { get; set; }
        public string Motion { get; set; }
        public string Source { get; set; }
        public string LegisId { get; set; }
        public int PrimaryLanguageId { get; set; }
        public Language PrimaryLanguage { get; set; }
        public List<DataModel.AmendmentBody> AmendmentBodies { get; set; } = new List<DataModel.AmendmentBody>();
    }
}
