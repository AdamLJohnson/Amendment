using Microsoft.AspNetCore.SignalR.Client;

namespace Amendment.Client.Helpers;
public class SignalRRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        Console.WriteLine("Retry Policy Hit");
        return TimeSpan.FromSeconds(5);
    }
}
