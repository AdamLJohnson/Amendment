namespace Amendment.Shared.Requests
{
    public sealed class CloneAmendmentRequest
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Motion { get; set; }
        public string? Source { get; set; }
        public string? LegisId { get; set; }
    }
}
