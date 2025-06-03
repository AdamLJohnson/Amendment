using System;

namespace Amendment.Shared.Responses
{
    public sealed class ClientErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
