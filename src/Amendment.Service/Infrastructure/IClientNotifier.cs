using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Service.Infrastructure
{
    public interface IClientNotifier
    {
        Task SendAsync(DestinationHub destinationHub, string method, object obj);
    }

    public enum DestinationHub
    {
        Amendment
    }
}
