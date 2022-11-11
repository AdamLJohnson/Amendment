using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Amendment.Model.Infrastructure;

namespace Amendment.Model.DataModel
{
    public class Role : ITableBase
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        public int EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
