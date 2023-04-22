using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Interfaces;
using Amendment.Shared.Responses;

namespace Amendment.Client.Repository
{
    public interface ITimerEventService
    {
        event EventHandler<SecondsUpdated>? SecondsUpdated;
        event EventHandler<StateUpdated>? StateUpdated;
        event EventHandler<ShowUpdated>? ShowUpdated;

        void OnSecondsUpdated(SecondsUpdated e);
        void OnStateUpdated(StateUpdated e);
        void OnShowUpdated(ShowUpdated e);
    }

    public class TimerEventService : ITimerEventService
    {
        public event EventHandler<SecondsUpdated>? SecondsUpdated;
        public event EventHandler<StateUpdated>? StateUpdated;
        public event EventHandler<ShowUpdated>? ShowUpdated;
        public void OnSecondsUpdated(SecondsUpdated e)
        {
            SecondsUpdated?.Invoke(this, e);
        }

        public void OnStateUpdated(StateUpdated e)
        {
            StateUpdated?.Invoke(this, e);
        }

        public void OnShowUpdated(ShowUpdated e)
        {
            ShowUpdated?.Invoke(this, e);
        }
    }
}
