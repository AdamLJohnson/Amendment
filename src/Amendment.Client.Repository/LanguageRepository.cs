using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;

namespace Amendment.Client.Repository
{
    public interface ILanguageRepository : IHttpRepository<LanguageRequest, LanguageResponse>
    {

    }
    public class LanguageRepository : HttpRepository<LanguageRequest, LanguageResponse>, ILanguageRepository
    {
        protected override string _baseUrl { get; set; } = "api/Language";
        private IEnumerable<LanguageResponse>? _languages;
        public LanguageRepository(HttpClient client) : base(client)
        {
        }

        public override async Task<IEnumerable<LanguageResponse>> GetAsync()
        {
            if (_languages != null)
                return _languages;

            var dbLanguages = await base.GetAsync();
            _languages = dbLanguages;
            return _languages;
        }
    }
}
