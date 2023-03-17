using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.SignalRCommands
{
    public class SetAmendmentBodyLiveCommands
    {
        public IEnumerable<SetAmendmentBodyLiveCommand> Commands { get; set; } = Enumerable.Empty<SetAmendmentBodyLiveCommand>();
    }
    public class SetAmendmentBodyLiveCommand
    {
        public int AmendId { get; set; }
        public int Id { get; set; }
        public bool IsLive { get; set; }
    }

    public class SetAmendmentBodyPageCommands
    {
        public IEnumerable<SetAmendmentBodyPageCommand> Commands { get; set; } = Enumerable.Empty<SetAmendmentBodyPageCommand>();
    }

    public class SetAmendmentBodyPageCommand
    {
        public int AmendId { get; set; }
        public int Id { get; set; }
        public int Page { get; set; }
    }
}
