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
        [Required]
        public string Username { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }

        public int EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public int LastUpdatedBy { get; set; }
        public DateTime LastUpdated { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public bool RequirePasswordChange { get; set; }


        public ICollection<Role> Roles { get; set; }
    }
}
