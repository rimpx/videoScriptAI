using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace videoscriptAI.Services
{
    public class VideoService : IVideoService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VideoService> _logger;

        public VideoService(IWebHostEnvironment environment, ILogger<VideoService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Carica un file video e lo salva nella cartella uploads
        /// </summary>
        public async Task<string> UploadVideoFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File non valido");

            // Verifica se il file è un video
            string[] allowedTypes = { "video/mp4", "video/mpeg", "video/mov", "video/avi",
                                      "video/x-flv", "video/mpg", "video/webm", "video/wmv", "video/3gpp" };

            bool isValidType = false;
            foreach (var type in allowedTypes)
            {
                if (file.ContentType.ToLower() == type)
                {
                    isValidType = true;
                    break;
                }
            }

            if (!isValidType)
                throw new ArgumentException("Formato file non supportato. Sono supportati solo file video.");

            // Crea la directory di uploads se non esiste
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "videos");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Genera un nome file unico
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Salva il file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"File video caricato: {uniqueFileName}");
            return filePath;
        }

        /// <summary>
        /// Verifica se l'URL è un link YouTube valido
        /// </summary>
        public bool IsValidYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // Pattern per identificare URL di YouTube
            var youtubeRegex = new Regex(
                @"^(https?:\/\/)?(www\.)?(youtube\.com\/watch\?v=|youtu\.be\/)([a-zA-Z0-9_-]{11}).*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return youtubeRegex.IsMatch(url);
        }

        /// <summary>
        /// Ottiene l'ID del video YouTube dall'URL
        /// </summary>
        public string GetYouTubeVideoId(string url)
        {
            if (!IsValidYouTubeUrl(url))
                return null;

            var youtubeRegex = new Regex(
                @"^(https?:\/\/)?(www\.)?(youtube\.com\/watch\?v=|youtu\.be\/)([a-zA-Z0-9_-]{11}).*$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var match = youtubeRegex.Match(url);
            if (match.Success)
            {
                return match.Groups[4].Value;
            }

            return null;
        }
    }
}