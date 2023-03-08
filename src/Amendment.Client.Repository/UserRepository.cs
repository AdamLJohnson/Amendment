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
    public interface IUserRepository : IHttpRepository<UserRequest, UserResponse>
    {

    }
    public class UserRepository : IUserRepository
    {
        private readonly JsonSerializerOptions _options;
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        public UserRepository(HttpClient client)
        {
            _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _client = client;
            _baseUrl = "api/User";
        }

        public async Task<IEnumerable<UserResponse>> GetAsync()
        {
            var response = await _client.GetAsync(_baseUrl);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return Enumerable.Empty<UserResponse>();

            var result = JsonSerializer.Deserialize<ApiResult<IEnumerable<UserResponse>>>(content, _options);
            return result == null ? Enumerable.Empty<UserResponse>() : result.Result;
        }

        public async Task<UserResponse> GetAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<UserResponse> PostAsync(UserRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<UserResponse> PutAsync(int id, UserRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
