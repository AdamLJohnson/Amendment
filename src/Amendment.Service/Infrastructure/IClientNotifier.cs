using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        Amendment,
        Screen
    }

    public static class ClientNotifierMethods
    {
        public const string AmendmentChange = "AmendmentChange";
        public const string AmendmentBodyChange = "AmendmentBodyChange";
        public const string GoLive = "GoLive";
        public const string GoLiveBody = "GoLiveBody";
        public const string ClearScreens = "ClearScreens";
    }

    public class ClientNotifierMethodsWrapper
    {
        public string AmendmentChange = ClientNotifierMethods.AmendmentChange;
        public string AmendmentBodyChange = ClientNotifierMethods.AmendmentBodyChange;
        public string GoLive = ClientNotifierMethods.GoLive;
        public string GoLiveBody = ClientNotifierMethods.GoLiveBody;
        public string ClearScreens = ClientNotifierMethods.ClearScreens;
    }
}
