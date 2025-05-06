using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using videoscriptAI.Services;
using Microsoft.AspNetCore.Http;
using videoscriptAI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using videoscriptAI.Models;

namespace videoscriptAI.Pages
{
    [Authorize]  // Aggiungiamo esplicitamente l'autorizzazione
    public class VideoActionsModel : PageModel
    {
        private readonly ILogger<VideoActionsModel> _logger;
        private readonly IVideoService _videoService;
        private readonly IAIService _aiService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public string VideoType { get; set; }
        public string VideoFilePath { get; set; }
        public string VideoFileName { get; set; }
        public string YoutubeUrl { get; set; }
        public string YoutubeVideoId { get; set; }
        public int ChatId { get; set; }
        public string ErrorMessage { get; set; }

        public VideoActionsModel(
            ILogger<VideoActionsModel> logger,
            IVideoService videoService,
            IAIService aiService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _videoService = videoService;
            _aiService = aiService;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Verifica che la chat esista e appartenga all'utente corrente
            var chat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
            if (chat == null)
            {
                return NotFound();
            }

            ChatId = id;

            // Recupera i dati dalla sessione
            VideoType = HttpContext.Session.GetString("VideoType") ?? "";

            try
            {
                if (VideoType == "youtube")
                {
                    YoutubeUrl = HttpContext.Session.GetString("YoutubeUrl") ?? "";
                    if (string.IsNullOrEmpty(YoutubeUrl))
                    {
                        throw new Exception("URL YouTube non trovato nella sessione");
                    }

                    YoutubeVideoId = _videoService.GetYouTubeVideoId(YoutubeUrl);
                    if (string.IsNullOrEmpty(YoutubeVideoId))
                    {
                        throw new Exception("Impossibile estrarre l'ID del video da YouTube URL");
                    }
                }
                else if (VideoType == "file")
                {
                    VideoFilePath = HttpContext.Session.GetString("VideoPath") ?? "";
                    if (string.IsNullOrEmpty(VideoFilePath) || !System.IO.File.Exists(VideoFilePath))
                    {
                        throw new Exception("File video non trovato");
                    }

                    VideoFileName = Path.GetFileName(VideoFilePath);
                }
                else
                {
                    throw new Exception("Tipo di video non valido");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella preparazione della pagina VideoActions");
                ErrorMessage = $"Si è verificato un errore: {ex.Message}";
                return RedirectToPage("Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action, string customPrompt, int chatId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var chat = await _dbContext.Chats
                .Include(c => c.ChatContents)
                .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == user.Id);

            if (chat == null)
            {
                return NotFound();
            }

            VideoType = HttpContext.Session.GetString("VideoType") ?? "";
            string prompt;
            string result;

            // Prepara il prompt in base all'azione selezionata
            switch (action)
            {
                case "summarize":
                    prompt = "Riassumi questo video evidenziando i punti principali e le informazioni più importanti.";
                    break;
                case "transcript":
                    prompt = "Genera una trascrizione completa del video con timestamp per i cambi di scena e le parti importanti.";
                    break;
                case "script":
                    prompt = "Crea uno script per un video simile a questo, mantenendo lo stesso stile e struttura, ma migliorato e ottimizzato.";
                    break;
                case "custom":
                    prompt = !string.IsNullOrWhiteSpace(customPrompt) ? customPrompt : "Analizza questo video e dimmi cosa ti colpisce di più.";
                    break;
                default:
                    prompt = "Descrivi brevemente cosa mostra questo video.";
                    break;
            }

            try
            {
                _logger.LogInformation($"Elaborazione video di tipo {VideoType} con azione: {action}");

                // Elabora il video con Gemini API
                if (VideoType == "youtube")
                {
                    YoutubeUrl = HttpContext.Session.GetString("YoutubeUrl") ?? "";
                    if (string.IsNullOrEmpty(YoutubeUrl))
                    {
                        throw new Exception("URL YouTube non trovato nella sessione");
                    }

                    _logger.LogInformation($"Elaborazione video YouTube: {YoutubeUrl}");
                    result = await _aiService.ProcessYoutubeVideoAsync(YoutubeUrl, prompt);
                }
                else // video file
                {
                    VideoFilePath = HttpContext.Session.GetString("VideoPath") ?? "";
                    if (string.IsNullOrEmpty(VideoFilePath) || !System.IO.File.Exists(VideoFilePath))
                    {
                        throw new Exception("File video non trovato");
                    }

                    _logger.LogInformation($"Elaborazione file video: {VideoFilePath}");
                    result = await _aiService.ProcessVideoFileAsync(VideoFilePath, prompt);
                }

                _logger.LogInformation("Elaborazione completata, salvataggio risultati");

                // Crea il messaggio dell'utente
                string userMessage;
                if (action == "custom")
                {
                    userMessage = $"Ho chiesto di analizzare il video con questa richiesta: {customPrompt}";
                }
                else
                {
                    userMessage = $"Ho chiesto di {GetActionDescription(action)} sul video.";
                }

                // Salva la richiesta dell'utente come messaggio nella chat
                var userChatContent = new ChatContent
                {
                    Content = userMessage,
                    IsFromUser = true,
                    CreatedAt = DateTime.Now,
                    ChatId = chatId
                };
                _dbContext.ChatContents.Add(userChatContent);

                // Salva la risposta dell'AI
                var aiChatContent = new ChatContent
                {
                    Content = result, // Assicurati che questo sia una stringa
                    IsFromUser = false,
                    CreatedAt = DateTime.Now,
                    ChatId = chatId
                };
                _dbContext.ChatContents.Add(aiChatContent);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Dati salvati nel database");

                // Pulisci la sessione
                HttpContext.Session.Remove("VideoType");
                HttpContext.Session.Remove("VideoPath");
                HttpContext.Session.Remove("YoutubeUrl");

                // Reindirizza alla pagina della chat
                return RedirectToPage("/Chat/Chat", new { id = chatId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del video");

                // Salva l'errore come messaggio nella chat
                var errorChatContent = new ChatContent
                {
                    Content = $"Si è verificato un errore durante l'elaborazione del video: {ex.Message}",
                    IsFromUser = false,
                    CreatedAt = DateTime.Now,
                    ChatId = chatId
                };
                _dbContext.ChatContents.Add(errorChatContent);
                await _dbContext.SaveChangesAsync();

                // Reindirizza alla pagina della chat con l'errore
                return RedirectToPage("/Chat/Chat", new { id = chatId });
            }
        }

        private string GetActionDescription(string action)
        {
            return action switch
            {
                "summarize" => "riassumere il contenuto",
                "transcript" => "generare la trascrizione",
                "script" => "creare uno script basato sul contenuto",
                _ => "analizzare il contenuto"
            };
        }
    }
}