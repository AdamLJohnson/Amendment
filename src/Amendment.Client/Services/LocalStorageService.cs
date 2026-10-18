using Microsoft.JSInterop;
using System.Text.Json;

namespace Amendment.Client.Services;

public interface ILocalStorageService
{
    ValueTask<T?> GetItemAsync<T>(string key);
    ValueTask<string?> GetItemAsStringAsync(string key);
    ValueTask SetItemAsync<T>(string key, T data);
    ValueTask SetItemAsStringAsync(string key, string data);
    ValueTask RemoveItemAsync(string key);
}

public class LocalStorageService : ILocalStorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IJSRuntime _jsRuntime;

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask<T?> GetItemAsync<T>(string key)
    {
        var raw = await GetItemAsStringAsync(key);
        if (string.IsNullOrWhiteSpace(raw))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(raw, SerializerOptions);
        }
        catch (JsonException) when (typeof(T) == typeof(string))
        {
            return (T)(object)raw;
        }
    }

    public ValueTask<string?> GetItemAsStringAsync(string key)
        => _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetItemAsync<T>(string key, T data)
        => SetItemAsStringAsync(key, JsonSerializer.Serialize(data, SerializerOptions));

    public ValueTask SetItemAsStringAsync(string key, string data)
        => _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, data);

    public ValueTask RemoveItemAsync(string key)
        => _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
}
