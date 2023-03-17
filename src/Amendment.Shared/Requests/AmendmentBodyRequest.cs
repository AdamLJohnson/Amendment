using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Enums;

namespace Amendment.Shared.Requests
{
    public sealed class AmendmentBodyRequest
    {
        private const string _pageSplit = "**NEWSLIDE**";
        public int SavingUserId { get; set; }
        public int LanguageId { get; set; }
        public string? AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; } = AmendmentBodyStatus.New;
        public bool IsLive { get; set; }
        public int Page { get; set; } = 0;

        public int ChangeBodyPage(int page)
        {
            if (page < 0 || page > Pages - 1)
                page = 0;
            Page = page;
            return Page;
        }

        public int Pages
        {
            get
            {
                if (string.IsNullOrEmpty(AmendBody))
                    return 0;

                return AmendBody.Split(_pageSplit).Length;
            }
        }
    }
}
