using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        Task<(AmendmentBodyResponse? response, string? errorMessage)> PostAsync(int amendmentId, AmendmentBodyRequest request);
        Task<AmendmentBodyResponse> PutAsync(int amendmentId, int id, AmendmentBodyRequest request);
        Task<(AmendmentBodyResponse? response, string? errorMessage)> PutWithErrorHandlingAsync(int amendmentId, int id, AmendmentBodyRequest request);
        Task DeleteAsync(int amendmentId, int id);
    }
    public class AmendmentBodyRepository : HttpRepository<AmendmentBodyRequest, AmendmentBodyResponse>, IAmendmentBodyRepository
    {
        protected override string BaseUrl { get; set; } = "api/AmendmentBody";
        private readonly ILogger<AmendmentBodyRepository> _logger;
        private readonly INotificationServiceWrapper _notificationServiceWrapper;

        public AmendmentBodyRepository(ILogger<AmendmentBodyRepository> logger, HttpClient client, INotificationServiceWrapper notificationServiceWrapper) : base(logger, client, notificationServiceWrapper)
        {
            _logger = logger;
            _notificationServiceWrapper = notificationServiceWrapper;
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

        public async Task<(AmendmentBodyResponse? response, string? errorMessage)> PostAsync(int amendmentId, AmendmentBodyRequest request)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";

            try
            {
                var json = JsonSerializer.Serialize(request);
                var bodyContent = new StringContent(json, Encoding.UTF8, "application/json");
                var httpResponse = await Client.PostAsync(BaseUrl, bodyContent);
                var content = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResult<AmendmentBodyResponse>>(content, Options);
                    return (result?.Result ?? new AmendmentBodyResponse(), null);
                }
                else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Parse validation errors from BadRequest response
                    var errorResult = JsonSerializer.Deserialize<ApiFailedResult<AmendmentBodyResponse>>(content, Options);
                    var errorMessage = errorResult?.Errors?.FirstOrDefault().Message ?? "Validation error occurred";
                    return (null, errorMessage);
                }
                else
                {
                    // For other error status codes, fall back to generic error handling
                    httpResponse.EnsureSuccessStatusCode();
                    return (new AmendmentBodyResponse(), null);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to access this url: POST {url}", BaseUrl);
                await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
                return (null, "An error has occurred. Please try again.");
            }
        }

        public Task<AmendmentBodyResponse> PutAsync(int amendmentId, int id, AmendmentBodyRequest request)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.PutAsync(id, request);
        }

        public async Task<(AmendmentBodyResponse? response, string? errorMessage)> PutWithErrorHandlingAsync(int amendmentId, int id, AmendmentBodyRequest request)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            var url = $"{BaseUrl}/{id}";
            try
            {
                var json = JsonSerializer.Serialize(request);
                var bodyContent = new StringContent(json, Encoding.UTF8, "application/json");
                var httpResponse = await Client.PutAsync(url, bodyContent);
                var content = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResult<AmendmentBodyResponse>>(content, Options);
                    return (result?.Result, null);
                }
                else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Parse validation errors from BadRequest response
                    var errorResult = JsonSerializer.Deserialize<ApiFailedResult<AmendmentBodyResponse>>(content, Options);
                    var errorMessage = errorResult?.Errors?.FirstOrDefault().Message ?? "Validation failed";
                    return (null, errorMessage);
                }
                else
                {
                    return (null, "An error occurred while updating the amendment body.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to access this url: PUT {url}", url);
                return (null, "An error has occurred. Please try again.");
            }
        }

        public Task DeleteAsync(int amendmentId, int id)
        {
            BaseUrl = $"api/Amendment/{amendmentId}/Body";
            return base.DeleteAsync(id);
        }
    }
}
