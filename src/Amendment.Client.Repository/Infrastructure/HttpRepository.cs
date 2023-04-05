using Amendment.Shared.Responses;
using Amendment.Shared;
using System.Text.Json;
using Amendment.Shared.Requests;
using System.Text;
using Microsoft.Extensions.Logging;
using System;

namespace Amendment.Client.Repository.Infrastructure;

public abstract class HttpRepository<TRequest, TResponse> : IHttpRepository<TRequest, TResponse> where TRequest : class, new() where TResponse : class, new()
{
    protected readonly JsonSerializerOptions Options;
    private readonly ILogger _logger;
    protected readonly HttpClient Client;
    private readonly INotificationServiceWrapper _notificationServiceWrapper;
    protected abstract string BaseUrl { get; set; }

    protected HttpRepository(ILogger logger, HttpClient client, INotificationServiceWrapper notificationServiceWrapper)
    {
        Options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _logger = logger;
        Client = client;
        _notificationServiceWrapper = notificationServiceWrapper;
    }

    public virtual async Task<IEnumerable<TResponse>> GetAsync()
    {
        try
        {
            var response = await Client.GetAsync(BaseUrl);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<ApiResult<IEnumerable<TResponse>>>(content, Options);
            return result?.Result ?? Enumerable.Empty<TResponse>();
        }
        catch (Exception e)
        {
            _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to trying to access this url: GET {url}", BaseUrl);
            await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
            return Enumerable.Empty<TResponse>();
        }
    }

    public virtual async Task<TResponse> GetAsync(int id)
    {
        var url = $"{BaseUrl}/{id}";
        try
        {
            var response = await Client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<ApiResult<TResponse>>(content, Options);
            return result?.Result ?? new TResponse();
        }
        catch (Exception e)
        {
            _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to trying to access this url: GET {url}", url);
            await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
            return new TResponse();
        }
    }

    public virtual async Task<TResponse> PostAsync(TRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var bodyContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync(BaseUrl, bodyContent);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
        

            var result = JsonSerializer.Deserialize<ApiResult<TResponse>>(content, Options);
            return result?.Result ?? new TResponse();
        }
        catch (Exception e)
        {
            _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to trying to access this url: POST {url}", BaseUrl);
            await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
            return new TResponse();
        }
    }

    public virtual async Task<TResponse> PutAsync(int id, TRequest request)
    {
        var url = $"{BaseUrl}/{id}";
        try
        {
            var json = JsonSerializer.Serialize(request);
            var bodyContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await Client.PutAsync(url, bodyContent);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<ApiResult<TResponse>>(content, Options);
            return result?.Result ?? new TResponse();
        }
        catch (Exception e)
        {
            _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to trying to access this url: PUT {url}", url);
            await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
            return new TResponse();
        }
    }

    public virtual async Task DeleteAsync(int id)
    {
        var url = $"{BaseUrl}/{id}";
        try
        {
            var response = await Client.DeleteAsync(url);
            var content = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(new EventId(1000, "RepositoryError"), e, "An error has occurred while trying to trying to access this url: DELETE {url}", url);
            await _notificationServiceWrapper.Error("An error has occurred. Please try again.", "Server Error");
        }
    }
}