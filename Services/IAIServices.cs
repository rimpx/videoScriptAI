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

        /// <summary>
        /// Invia un URL YouTube all'API e ottiene un'analisi
        /// </summary>
        /// <param name="youtubeUrl">URL del video YouTube</param>
        /// <param name="prompt">Il prompt per specificare cosa fare con il video</param>
        /// <returns>La risposta generata dall'AI</returns>
        Task<string> ProcessYoutubeVideoAsync(string youtubeUrl, string prompt);

        /// <summary>
        /// Invia un file video all'API e ottiene un'analisi
        /// </summary>
        /// <param name="filePath">Percorso del file video</param>
        /// <param name="prompt">Il prompt per specificare cosa fare con il video</param>
        /// <returns>La risposta generata dall'AI</returns>
        Task<string> ProcessVideoFileAsync(string filePath, string prompt);
    }
}