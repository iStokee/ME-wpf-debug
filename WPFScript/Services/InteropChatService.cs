using System.Collections;
using System.Collections.Generic;
using MESharp.Models;
using csharp_interop.csharp_api;

namespace MESharp.Services
{
    public sealed class InteropChatService : IChatService
    {
        public IReadOnlyList<ChatMessage> GetMessages()
        {
            var source = Chat.GetMessages();
            if (source == null)
            {
                return System.Array.Empty<ChatMessage>();
            }

            var items = source is ICollection collection
                ? new List<ChatMessage>(collection.Count)
                : new List<ChatMessage>();

            foreach (var message in source)
            {
                items.Add(new ChatMessage
                {
                    Timestamp = message.Timestamp,
                    Name = message.Name ?? string.Empty,
                    Text = message.Text ?? string.Empty
                });
            }

            return items;
        }
    }
}
