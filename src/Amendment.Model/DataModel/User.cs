using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Amendment.Model.Infrastructure;

namespace Amendment.Model.DataModel
{
    public class User : ITableBase
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public int EntryBy { get; set; }
        public DateTime EntryDate { get; set; }
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdated { get; set; }


        public List<UserXRole> UserXRoles { get; set; }
    }
}
