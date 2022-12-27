using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Service.Infrastructure
{
    public sealed class MockClientNotifier : IClientNotifier
    {
        public Task SendToAllAsync(DestinationHub destinationHub, string method, object obj)
        {
            return Task.CompletedTask;
        }

        public Task SendToLanguageScreenAsync(int languageId, string method, object obj)
        {
            return Task.CompletedTask;
        }
    }
}
