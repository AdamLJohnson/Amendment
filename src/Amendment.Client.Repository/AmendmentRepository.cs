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
    public interface IAmendmentRepository : IHttpRepository<AmendmentRequest, AmendmentResponse>
    {
        Task<AmendmentFullBodyResponse?> GetLiveAsync();
    }
    public class AmendmentRepository : HttpRepository<AmendmentRequest, AmendmentResponse>, IAmendmentRepository
    {
        private readonly ILogger<AmendmentRepository> _logger;
        private readonly INotificationServiceWrapper _notificationServiceWrapper;
        protected override string _baseUrl { get; set; } = "api/Amendment";
        public AmendmentRepository(ILogger<AmendmentRepository> logger, HttpClient client, INotificationServiceWrapper notificationServiceWrapper) : base(logger, client, notificationServiceWrapper)
        {
            _logger = logger;
            _notificationServiceWrapper = notificationServiceWrapper;
        }

        public async Task<AmendmentFullBodyResponse?> GetLiveAsync()
        {
            var url = $"api/Amendment/Live";
            try
            {
                var response = await _client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();

                var result = JsonSerializer.Deserialize<ApiResult<AmendmentFullBodyResponse?>>(content, _options);
                return result?.Result;
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to trying to access this url: GET {url}", url);
                await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
                return null;
            }
        }
    }
}
