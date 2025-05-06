using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace videoscriptAI.Services
{
    public interface IVideoService
    {
        /// <summary>
        /// Carica un file video e lo prepara per l'elaborazione
        /// </summary>
        Task<string> UploadVideoFileAsync(IFormFile file);

        /// <summary>
        /// Verifica se l'URL è un link YouTube valido
        /// </summary>
        bool IsValidYouTubeUrl(string url);

        /// <summary>
        /// Ottiene l'ID del video YouTube dall'URL
        /// </summary>
        string GetYouTubeVideoId(string url);
    }
}