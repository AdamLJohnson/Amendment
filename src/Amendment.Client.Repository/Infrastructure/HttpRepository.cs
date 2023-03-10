using Amendment.Shared.Responses;
using Amendment.Shared;
using System.Text.Json;
using Amendment.Shared.Requests;
using System.Text;

namespace Amendment.Client.Repository.Infrastructure;

public abstract class HttpRepository<TRequest, TResponse> : IHttpRepository<TRequest, TResponse> where TRequest : class, new() where TResponse : class, new()
{
    protected readonly JsonSerializerOptions _options;
    protected readonly HttpClient _client;
    protected abstract string _baseUrl { get; set; }

    protected HttpRepository(HttpClient client)
    {
        _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _client = client;
    }

    public virtual async Task<IEnumerable<TResponse>> GetAsync()
    {
        var response = await _client.GetAsync(_baseUrl);
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<ApiResult<IEnumerable<TResponse>>>(content, _options);
        return result == null ? Enumerable.Empty<TResponse>() : result.Result;
    }

    public virtual async Task<TResponse> GetAsync(int id)
    {
        var response = await _client.GetAsync($"{_baseUrl}/{id}");
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<ApiResult<TResponse>>(content, _options);
        return result == null ? new TResponse() : result.Result;
    }

    public virtual async Task<TResponse> PostAsync(TRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var bodyContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(_baseUrl, bodyContent);
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<ApiResult<TResponse>>(content, _options);
        return result == null ? new TResponse() : result.Result;
    }

    public virtual async Task<TResponse> PutAsync(int id, TRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var bodyContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"{_baseUrl}/{id}", bodyContent);
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var result = JsonSerializer.Deserialize<ApiResult<TResponse>>(content, _options);
        return result == null ? new TResponse() : result.Result;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var response = await _client.DeleteAsync($"{_baseUrl}/{id}");
        var content = await response.Content.ReadAsStringAsync();
    }
}