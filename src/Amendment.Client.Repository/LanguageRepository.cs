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
using Microsoft.Extensions.Logging;

namespace Amendment.Client.Repository
{
    public interface ILanguageRepository : IHttpRepository<LanguageRequest, LanguageResponse>
    {

    }
    public class LanguageRepository : HttpRepository<LanguageRequest, LanguageResponse>, ILanguageRepository
    {
        protected override string BaseUrl { get; set; } = "api/Language";
        private IEnumerable<LanguageResponse>? _languages;
        public LanguageRepository(ILogger<LanguageRepository> logger, HttpClient client, INotificationServiceWrapper notificationServiceWrapper) : base(logger, client, notificationServiceWrapper)
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
