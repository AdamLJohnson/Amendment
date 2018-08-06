using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Amendment.Model.DataModel
{
    public class UserXRole
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int RoleId { get; set; }

        public User User { get; set; }
        public Role Role { get; set; }
    }
}
