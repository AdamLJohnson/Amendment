﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.Requests
{
    public sealed class AmendmentRequest
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Motion { get; set; }
        public string? Source { get; set; }
        public string? LegisId { get; set; }
        public int PrimaryLanguageId { get; set; }
        public bool IsLive { get; set; }
        public bool IsArchived { get; set; }
        public bool IsApproved { get; set; }
        public int? ParentAmendmentId { get; set; }
        public bool UpdateCurrentAmendmentBodies { get; set; } = true;
    }
}
