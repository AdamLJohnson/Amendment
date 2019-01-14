using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Model.Enums;

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
        public bool IsLive { get; set; }
        public int PrimaryLanguageId { get; set; }
        public Language PrimaryLanguage { get; set; }
        public List<AmendmentListBody> AmendmentBodies { get; set; } = new List<AmendmentListBody>();
    }

    public class AmendmentListBody
    {
        public int Id { get; set; }
        public int AmendId { get; set; }
        public int LanguageId { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
        public bool IsLive { get; set; }
        public Language Language { get; set; }
    }
}
