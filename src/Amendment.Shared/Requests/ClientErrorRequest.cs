using System;

namespace Amendment.Shared.Requests
{
    public sealed class ClientErrorRequest
    {
        public string ErrorMessage { get; set; } = "";
        public string? StackTrace { get; set; }
        public string Url { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? AdditionalContext { get; set; }
        public string ErrorType { get; set; } = "JavaScript";
        public string? ComponentName { get; set; }
        public string? UserAction { get; set; }
    }
}
