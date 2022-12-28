using System.ComponentModel.DataAnnotations;

namespace Amendment.Shared.Requests
{
    public sealed class AccountLoginRequest
    {
        [Required]
        public string? Username { get; set; }
        [Required]
        public string? Password { get; set; }
    }
}
