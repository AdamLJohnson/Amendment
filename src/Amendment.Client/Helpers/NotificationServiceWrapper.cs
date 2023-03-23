using Amendment.Client.Repository.Infrastructure;
using Blazorise;

namespace Amendment.Client.Helpers
{
    public class NotificationServiceWrapper : INotificationServiceWrapper
    {
        private INotificationService _notificationService;

        public NotificationServiceWrapper(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Info(string message, string? title = null)
        {
            await _notificationService.Info(message, title);
        }

        public async Task Success(string message, string? title = null)
        {
            await _notificationService.Success(message, title);
        }

        public async Task Warning(string message, string? title = null)
        {
            await _notificationService.Warning(message, title);
        }

        public async Task Error(string message, string? title = null)
        {
            await _notificationService.Error(message, title);
        }
    }
}
