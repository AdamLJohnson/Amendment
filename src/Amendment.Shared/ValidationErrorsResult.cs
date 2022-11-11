using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared
{
    public sealed class ValidationErrorsResult
    {
        public string PropertyName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
