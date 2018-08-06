using System;
using System.Collections.Generic;
using System.Text;

namespace Amendment.Model.Infrastructure
{
    public interface IReadOnlyTable
    {
        int Id { get; set; }
    }
}
