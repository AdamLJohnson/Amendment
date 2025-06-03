using Amendment.Shared.Requests;

namespace Amendment.Client.Services
{
    public interface IClientErrorService
    {
        Task LogErrorAsync(ClientErrorRequest errorRequest);
        Task LogErrorAsync(string errorMessage, string? stackTrace = null, string? componentName = null, string? userAction = null, string? additionalContext = null);
        Task LogJavaScriptErrorAsync(string errorMessage, string? stackTrace = null, string? url = null, string? additionalContext = null);
    }
}
