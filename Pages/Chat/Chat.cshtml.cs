using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using videoscriptAI.Data;
using videoscriptAI.Models;
using videoscriptAI.Services;

namespace videoscriptAI.Pages.Chat
{
    [Authorize]
    public class ChatModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAIService _aiService;
        private readonly ILogger<ChatModel> _logger;

        public List<videoscriptAI.Data.Chat> ChatHistory { get; set; }
        public videoscriptAI.Data.Chat CurrentChat { get; set; }
        public List<ChatContent> ChatContents { get; set; }

        public ChatModel(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IAIService aiService,
            ILogger<ChatModel> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _aiService = aiService;
            _logger = logger;
            ChatHistory = new List<videoscriptAI.Data.Chat>();
            ChatContents = new List<ChatContent>();
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Carica la cronologia delle chat dell'utente
            ChatHistory = await _dbContext.Chats
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Carica la chat corrente se è stata specificata
            if (id > 0)
            {
                CurrentChat = await _dbContext.Chats
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

                if (CurrentChat == null)
                {
                    return NotFound();
                }

                // Carica i messaggi della chat
                ChatContents = await _dbContext.ChatContents
                    .Where(c => c.ChatId == id)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int chatId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Il messaggio non può essere vuoto.");
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Verifica che la chat esista e appartenga all'utente
            var chat = await _dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == user.Id);

            if (chat == null)
            {
                return NotFound();
            }

            try
            {
                // Salva il messaggio dell'utente
                var userChatContent = new ChatContent
                {
                    Content = message,
                    IsFromUser = true,
                    CreatedAt = DateTime.Now,
                    ChatId = chatId
                };
                _dbContext.ChatContents.Add(userChatContent);
                await _dbContext.SaveChangesAsync();

                // Carica tutti i messaggi della chat per inviarli all'IA
                var chatHistory = await _dbContext.ChatContents
                    .Where(c => c.ChatId == chatId)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                // Converte ChatContent in ChatMessage per l'API
                var chatMessages = chatHistory.Select(m => new ChatMessage(m.Content, m.IsFromUser)).ToList();

                // Ottieni la risposta dall'AI
                string aiResponse = await _aiService.GetResponseAsync(chatMessages);

                // Salva la risposta dell'AI
                var aiChatContent = new ChatContent
                {
                    Content = aiResponse,
                    IsFromUser = false,
                    CreatedAt = DateTime.Now,
                    ChatId = chatId
                };
                _dbContext.ChatContents.Add(aiChatContent);

                // RIMOSSA LA GENERAZIONE AUTOMATICA DEL TITOLO
                // Il codice per la generazione automatica del titolo è stato rimosso da qui

                await _dbContext.SaveChangesAsync();
                return RedirectToPage("/Chat/Chat", new { id = chatId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del messaggio");
                ModelState.AddModelError("", $"Si è verificato un errore: {ex.Message}");

                // Ricarica i dati necessari per visualizzare la pagina
                await OnGetAsync(chatId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateTitleAsync(int chatId, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle))
            {
                ModelState.AddModelError("", "Il titolo non può essere vuoto.");
                await OnGetAsync(chatId);
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Verifica che la chat esista e appartenga all'utente
            var chat = await _dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == user.Id);

            if (chat == null)
            {
                return NotFound();
            }

            try
            {
                // Aggiorna il titolo e imposta IsAutoGenerated a false poiché è stato modificato manualmente
                chat.Title = newTitle.Trim();
                chat.IsAutoGenerated = false;
                _dbContext.Chats.Update(chat);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Titolo aggiornato manualmente per la chat {chatId}: {newTitle}");

                return RedirectToPage("/Chat/Chat", new { id = chatId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento del titolo");
                ModelState.AddModelError("", $"Si è verificato un errore: {ex.Message}");

                await OnGetAsync(chatId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRegenerateTitleAsync(int chatId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Verifica che la chat esista e appartenga all'utente
            var chat = await _dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == user.Id);

            if (chat == null)
            {
                return NotFound();
            }

            try
            {
                // Carica i messaggi della chat per inviarli all'IA
                var chatHistory = await _dbContext.ChatContents
                    .Where(c => c.ChatId == chatId)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                if (chatHistory.Count < 2)
                {
                    // Non ci sono abbastanza messaggi per generare un titolo significativo
                    ModelState.AddModelError("", "Non ci sono abbastanza messaggi per generare un titolo significativo");
                    await OnGetAsync(chatId);
                    return Page();
                }

                // Genera un titolo basato sul contenuto della chat
                string titlePrompt = "Genera un titolo breve e descrittivo (massimo 50 caratteri) per questa conversazione basandoti sul contenuto. Ritorna solo il titolo senza virgolette o spiegazioni.";
                var titleMessages = new List<ChatMessage>
        {
            new ChatMessage(titlePrompt, true)
        };

                // Aggiungi alcuni messaggi recenti per contesto
                var recentMessages = chatHistory.TakeLast(Math.Min(5, chatHistory.Count));
                foreach (var msg in recentMessages)
                {
                    titleMessages.Add(new ChatMessage(msg.Content, msg.IsFromUser));
                }

                string generatedTitle = await _aiService.GetResponseAsync(titleMessages);
                generatedTitle = generatedTitle.Trim().TrimEnd('.').TrimEnd();
                if (generatedTitle.Length > 50)
                    generatedTitle = generatedTitle.Substring(0, 47) + "...";

                chat.Title = generatedTitle;
                chat.IsAutoGenerated = true;
                _dbContext.Chats.Update(chat);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Titolo rigenerato per la chat {chatId}: {generatedTitle}");

                return RedirectToPage("/Chat/Chat", new { id = chatId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la rigenerazione del titolo");
                ModelState.AddModelError("", $"Si è verificato un errore: {ex.Message}");

                await OnGetAsync(chatId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteChatAsync(int chatId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Verifica che la chat esista e appartenga all'utente
            var chat = await _dbContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == user.Id);

            if (chat == null)
            {
                return NotFound();
            }

            try
            {
                // Elimina prima tutti i contenuti della chat
                var chatContents = await _dbContext.ChatContents
                    .Where(c => c.ChatId == chatId)
                    .ToListAsync();

                _dbContext.ChatContents.RemoveRange(chatContents);

                // Poi elimina la chat
                _dbContext.Chats.Remove(chat);
                await _dbContext.SaveChangesAsync();

                // Reindirizza alla pagina delle chat (se c'è almeno una chat rimanente)
                var nextChat = await _dbContext.Chats
                    .Where(c => c.UserId == user.Id)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (nextChat != null)
                {
                    return RedirectToPage("/Chat/Chat", new { id = nextChat.Id });
                }
                else
                {
                    // Se non ci sono altre chat, vai alla home
                    return RedirectToPage("/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione della chat");
                ModelState.AddModelError("", $"Si è verificato un errore: {ex.Message}");

                await OnGetAsync(chatId);
                return Page();
            }
        }
    }
}