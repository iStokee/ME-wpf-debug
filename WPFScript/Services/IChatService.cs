using System.Collections.Generic;
using MESharp.Models;

namespace MESharp.Services
{
    public interface IChatService
    {
        IReadOnlyList<ChatMessage> GetMessages();
    }
}
