using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Enums;
using Amendment.Shared.Requests;

namespace Amendment.Shared.Responses
{
    public sealed class AmendmentBodyResponse
    {
        public int Id { get; set; }
        public int AmendId { get; set; }
        public int LanguageId { get; set; }
        public string AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
        public bool IsLive { get; set; }

        public AmendmentBodyRequest ToRequest()
        {
            return new AmendmentBodyRequest
            {
                LanguageId = LanguageId,
                AmendBody = AmendBody,
                AmendStatus = AmendStatus
            };
        }
    }
}
