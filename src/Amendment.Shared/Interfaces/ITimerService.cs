using Amendment.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.Interfaces
{

    public interface ITimerService
    {
        void Reset();
        void Reset(int seconds);
        void Set(int seconds);
        void Pause();
        void Start();
        void Show(bool show);
        int Seconds { get; }
        bool IsRunning { get; }
        bool Shown { get; }
        event EventHandler<SecondsUpdated> SecondsUpdated;
        event EventHandler<StateUpdated> StateUpdated;
    }
}
