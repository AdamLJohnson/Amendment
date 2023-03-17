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
        private const string _pageSplit = "**NEWSLIDE**";
        public int Id { get; set; }
        public int AmendId { get; set; }
        public int LanguageId { get; set; }
        public string AmendBody { get; set; }
        public AmendmentBodyStatus AmendStatus { get; set; }
        public bool IsLive { get; set; }
        public int Page { get; set; } = 0;

        public AmendmentBodyRequest ToRequest()
        {
            return new AmendmentBodyRequest
            {
                LanguageId = LanguageId,
                AmendBody = AmendBody,
                AmendStatus = AmendStatus,
                IsLive = IsLive,
                Page = Page
            };
        }

        public int ChangeBodyPage(int page)
        {
            if (page < 0 || page > Pages - 1)
                page = 0;
            Page = page;
            return Page;
        }

        public string AmendBodyPagedText
        {
            get
            {
                if (string.IsNullOrEmpty(AmendBody))
                    return "";

                var pages = AmendBody.Split(_pageSplit);
                if (Page > Pages)
                    return pages[0];

                if (Page < pages.Length)
                    return pages[Page];

                return "";
            }
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
