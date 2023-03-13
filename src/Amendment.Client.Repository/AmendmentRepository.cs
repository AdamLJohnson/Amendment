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
    public interface IAmendmentRepository : IHttpRepository<AmendmentRequest, AmendmentResponse>
    {
        Task<AmendmentFullBodyResponse?> GetLiveAsync();
    }
    public class AmendmentRepository : HttpRepository<AmendmentRequest, AmendmentResponse>, IAmendmentRepository
    {
        protected override string _baseUrl { get; set; } = "api/Amendment";
        public AmendmentRepository(HttpClient client) : base(client)
        {
        }

        public async Task<AmendmentFullBodyResponse?> GetLiveAsync()
        {
            var response = await _client.GetAsync($"api/Amendment/Live");
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<ApiResult<AmendmentFullBodyResponse?>>(content, _options);
            return result?.Result;
        }
    }
}
