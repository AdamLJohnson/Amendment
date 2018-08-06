using System;
using System.Collections.Generic;
using System.Text;
using Amendment.Model.Infrastructure;

namespace Amendment.Model.DataModel
{
    public class Role : ITableBase
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int EntryBy { get; set; }
        public DateTime EntryDate { get; set; }
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdated { get; set; }

        public List<UserXRole> UserXRoles { get; set; }
    }
}
