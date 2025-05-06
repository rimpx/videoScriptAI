using System.Collections.Generic;
using System.Threading.Tasks;
using videoscriptAI.Models;

namespace videoscriptAI.Services
{
    public interface IAIService
    {
        /// <summary>
        /// Invia una lista di messaggi all'AI e ottiene una risposta
        /// </summary>
        /// <param name="messages">Lista di messaggi della chat</param>
        /// <returns>La risposta generata dall'AI</returns>
        Task<string> GetResponseAsync(List<ChatMessage> messages);
    }
}