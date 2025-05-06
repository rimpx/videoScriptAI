using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using videoscriptAI.Services;
using Microsoft.AspNetCore.Identity;
using videoscriptAI.Data;
using System.Linq;
using videoscriptAI.Models;

namespace videoscriptAI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IVideoService _videoService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        [TempData]
        public string ErrorMessage { get; set; }

        public IndexModel(
            ILogger<IndexModel> logger,
            IVideoService videoService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _videoService = videoService;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostUploadVideoAsync(IFormFile videoFile)
        {
            if (videoFile == null || videoFile.Length == 0)
            {
                ErrorMessage = "Seleziona un file video da caricare.";
                return Page();
            }

            if (videoFile.Length > 20 * 1024 * 1024) // 20MB
            {
                ErrorMessage = "Il file è troppo grande. Il limite è di 20MB. Per file più grandi, usa un URL YouTube.";
                return Page();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Carica il file
                string filePath = await _videoService.UploadVideoFileAsync(videoFile);

                // Crea una nuova chat
                var chat = new videoscriptAI.Data.Chat
                {
                    Title = $"Analisi video: {Path.GetFileName(videoFile.FileName)}",
                    UserId = user.Id,
                    CreatedAt = DateTime.Now
                };

                _dbContext.Chats.Add(chat);
                await _dbContext.SaveChangesAsync();

                // Salva il percorso del file e altre informazioni nella sessione
                HttpContext.Session.SetString("VideoPath", filePath);
                HttpContext.Session.SetString("VideoType", "file");
                HttpContext.Session.SetInt32("ChatId", chat.Id);

                return RedirectToPage("VideoActions", new { id = chat.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il caricamento del video");
                ErrorMessage = $"Si è verificato un errore: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostProcessYoutubeUrlAsync(string youtubeUrl)
        {
            if (string.IsNullOrWhiteSpace(youtubeUrl))
            {
                ErrorMessage = "Inserisci un URL YouTube valido.";
                return Page();
            }

            if (!_videoService.IsValidYouTubeUrl(youtubeUrl))
            {
                ErrorMessage = "URL YouTube non valido.";
                return Page();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var videoId = _videoService.GetYouTubeVideoId(youtubeUrl);

                // Crea una nuova chat con gestione più robusta degli errori di database
                videoscriptAI.Data.Chat chat;
                try
                {
                    chat = new videoscriptAI.Data.Chat
                    {
                        Title = $"Analisi video YouTube: {videoId}",
                        UserId = user.Id,
                        CreatedAt = DateTime.Now
                    };

                    _dbContext.Chats.Add(chat);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Chat creata con ID {chat.Id} per il video YouTube {videoId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante la creazione della chat nel database");
                    ErrorMessage = "Impossibile creare una nuova chat. Problema di connessione al database.";
                    return Page();
                }

                // Salva l'URL e altre informazioni nella sessione
                HttpContext.Session.SetString("YoutubeUrl", youtubeUrl);
                HttpContext.Session.SetString("VideoType", "youtube");
                HttpContext.Session.SetInt32("ChatId", chat.Id);

                return RedirectToPage("VideoActions", new { id = chat.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione dell'URL YouTube");
                ErrorMessage = $"Si è verificato un errore: {ex.Message}";
                return Page();
            }
        }
    }
}