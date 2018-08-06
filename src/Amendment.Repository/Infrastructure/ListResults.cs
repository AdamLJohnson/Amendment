using System;
using System.Collections.Generic;
using System.Text;

namespace Amendment.Repository.Infrastructure
{
    public class ListResults<T>
    {
        public List<T> Results { get; set; }
        public int PageNumber { get; set; }
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
    }
}
