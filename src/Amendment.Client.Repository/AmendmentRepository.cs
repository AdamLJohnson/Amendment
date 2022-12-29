using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amendment.Client.Repository.Infrastructure;
using Amendment.Shared;
using Amendment.Shared.Requests;
using Amendment.Shared.Responses;

namespace Amendment.Client.Repository
{
    public interface IAmendmentRepository : IHttpRepository<AmendmentRequest, AmendmentResponse>
    {

    }
    public class AmendmentRepository : IAmendmentRepository
    {
        private readonly JsonSerializerOptions _options;
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        public AmendmentRepository(HttpClient client)
        {
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _client = client;
            _baseUrl = "api/Amendment";
        }

        public async Task<IEnumerable<AmendmentResponse>> GetAsync()
        {
            var response = await _client.GetAsync(_baseUrl);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<AmendmentResponse>();

            var result = JsonSerializer.Deserialize<ApiResult<IEnumerable<AmendmentResponse>>>(content, _options);
            return result == null ? Enumerable.Empty<AmendmentResponse>() : result.Result;
        }

        public async Task<AmendmentResponse> GetAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<AmendmentResponse> PostAsync(AmendmentRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<AmendmentResponse> PutAsync(int id, AmendmentRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
