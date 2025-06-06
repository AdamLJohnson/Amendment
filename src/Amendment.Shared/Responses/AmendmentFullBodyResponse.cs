using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Enums;
using Amendment.Shared.Requests;

namespace Amendment.Shared.Responses
{
    public sealed class AmendmentFullBodyResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Motion { get; set; } = "";
        public string Source { get; set; } = "";
        public string LegisId { get; set; } = "";
        public bool IsLive { get; set; }
        public bool IsArchived { get; set; }
        public bool IsApproved { get; set; }
        public int? ParentAmendmentId { get; set; }
        public List<AmendmentBodyResponse> AmendmentBodies { get; set; } = new ();


        public int PrimaryLanguageId { get; set; }

        public AmendmentRequest ToRequest()
        {
            return new AmendmentRequest
            {
                Title = Title,
                Author = Author,
                Motion = Motion,
                Source = Source,
                LegisId = LegisId,
                PrimaryLanguageId = PrimaryLanguageId,
                IsLive = IsLive,
                IsArchived = IsArchived,
                IsApproved = IsApproved,
                ParentAmendmentId = ParentAmendmentId,
                UpdateCurrentAmendmentBodies = true // Default to true for existing behavior
            };
        }
    }
}
