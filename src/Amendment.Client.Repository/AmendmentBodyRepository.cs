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
    public interface IAmendmentBodyRepository
    {
        Task<IEnumerable<AmendmentBodyResponse>> GetAsync(int amendmentId);
        Task<AmendmentBodyResponse> GetAsync(int amendmentId, int id);
        Task<AmendmentBodyResponse> PostAsync(int amendmentId, AmendmentBodyRequest request);
        Task<AmendmentBodyResponse> PutAsync(int amendmentId, int id, AmendmentBodyRequest request);
        Task DeleteAsync(int amendmentId, int id);
    }
    public class AmendmentBodyRepository : HttpRepository<AmendmentBodyRequest, AmendmentBodyResponse>, IAmendmentBodyRepository
    {
        protected override string BaseUrl { get; set; } = "api/AmendmentBody";
        public AmendmentBodyRepository(ILogger<AmendmentBodyRepository> logger, HttpClient client, INotificationServiceWrapper notificationServiceWrapper) : base(logger, client, notificationServiceWrapper)
        {
        }

        Task<IEnumerable<AmendmentBodyResponse>> IAmendmentBodyRepository.GetAsync(int amendmentId)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.GetAsync();
        }

        public Task<AmendmentBodyResponse> GetAsync(int amendmentId, int id)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.GetAsync(id);
        }

        public Task<AmendmentBodyResponse> PostAsync(int amendmentId, AmendmentBodyRequest request)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.PostAsync(request);
        }

        public Task<AmendmentBodyResponse> PutAsync(int amendmentId, int id, AmendmentBodyRequest request)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.PutAsync(id, request);
        }

        public Task DeleteAsync(int amendmentId, int id)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.DeleteAsync(id);
        }
    }
}
