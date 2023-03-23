using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Client.Repository.Infrastructure
{
    public interface INotificationServiceWrapper
    {
        Task Info(string message, string? title = null);
        Task Success(string message, string? title = null);
        Task Warning(string message, string? title = null);
        Task Error(string message, string? title = null);
    }
}

