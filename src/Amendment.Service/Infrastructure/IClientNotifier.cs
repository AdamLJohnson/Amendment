using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Service.Infrastructure
{
    public interface IClientNotifier
    {
        Task SendToAllAsync(DestinationHub destinationHub, string method, object obj);
        Task SendToLanguageScreenAsync(int languageId, string method, object obj);
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
        public const string ClearScreens = "ClearScreens";
        public const string RefreshLanguage = "RefreshLanguage";
    }

    public class ClientNotifierMethodsWrapper
    {
        public string AmendmentChange = ClientNotifierMethods.AmendmentChange;
        public string AmendmentBodyChange = ClientNotifierMethods.AmendmentBodyChange;
        public string ClearScreens = ClientNotifierMethods.ClearScreens;
        public string RefreshLanguage = ClientNotifierMethods.RefreshLanguage;
    }
}
