using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.Requests
{
    public sealed class SystemSettingRequest
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
