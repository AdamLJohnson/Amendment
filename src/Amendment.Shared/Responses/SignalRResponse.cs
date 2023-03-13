using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amendment.Shared.Enums;

namespace Amendment.Shared.Responses
{
    public record SignalRResponse<T>(OperationType OperationType, T Value);
}
