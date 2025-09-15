namespace MESharp.Models
{
    public sealed class ChatMessage
    {
        public ulong Timestamp { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
    }
}
