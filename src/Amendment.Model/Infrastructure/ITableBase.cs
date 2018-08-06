using System;
using System.Collections.Generic;
using System.Text;

namespace Amendment.Model.Infrastructure
{
    public interface ITableBase
    {
        int Id { get; }
        int EntryBy { get; set; }
        DateTime EntryDate { get; set; }
        int LastUpdatedBy { get; set; }
        DateTime LastUpdated { get; set; }
    }
}
