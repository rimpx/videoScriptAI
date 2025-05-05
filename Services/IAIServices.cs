using System.Collections.Generic;
using System.Threading.Tasks;
using videoscriptAI.Models;

namespace videoscriptAI.Services
{
    public interface IAIService
    {
        Task<string> GetResponseAsync(List<ChatMessage> messages);
    }
}