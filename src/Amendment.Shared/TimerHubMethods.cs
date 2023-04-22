using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared
{
    public static class TimerHubMethods
    {
        public const string StateChangedEvent = "Timer.StateChange";
        public const string ShowChangedEvent = "Timer.ShowChange";
        public const string SecondsChangedEvent = "Timer.SecondsChange";
    }
}
