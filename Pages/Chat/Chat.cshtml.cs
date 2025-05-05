using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using videoscriptAI.Services;
using Microsoft.AspNetCore.Identity;
using videoscriptAI.Data;
using Microsoft.EntityFrameworkCore;
using videoscriptAI.Models;

namespace videoscriptAI.Pages
{
    public class ChatModel : PageModel
    {
        private readonly ILogger<ChatModel> _logger;
        private readonly GeminiService _geminiService;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        [BindProperty]
        public string NewMessage { get; set; }

        public List<ChatMessageViewModel> ChatMessages { get; set; } = new List<ChatMessageViewModel>();
        public List<ChatHistoryItem> ChatHistory { get; set; } = new List<ChatHistoryItem>();
        public int CurrentChatId { get; set; }
        public bool IsProcessing { get; set; } = false;

        public ChatModel(
            ILogger<ChatModel> logger,
            GeminiService geminiService,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _geminiService = geminiService;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(int? id = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Load chat history
            ChatHistory = await _dbContext.Chats
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ChatHistoryItem
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            // If id is provided, load that specific chat
            if (id.HasValue)
            {
                var chat = await _dbContext.Chats
                    .Include(c => c.ChatContents)
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

                if (chat != null)
                {
                    CurrentChatId = chat.Id;
                    ChatMessages = chat.ChatContents
                        .OrderBy(m => m.CreatedAt)
                        .Select(m => new ChatMessageViewModel
                        {
                            Text = m.Content,
                            IsUser = m.IsFromUser,
                            Timestamp = m.CreatedAt
                        })
                        .ToList();

                    return Page();
                }
            }

            // If no chat is selected or the requested chat doesn't exist, start a new chat
            if (!ChatMessages.Any())
            {
                ChatMessages.Add(new ChatMessageViewModel
                {
                    Text = "Hello! I'm your Video Script AI Assistant. How can I help you today? You can ask me to:\n\n" +
                          "• Generate a script outline for your video\n" +
                          "• Suggest improvements to your existing script\n" +
                          "• Create engaging introductions or conclusions\n" +
                          "• Optimize your script for specific platforms",
                    IsUser = false,
                    Timestamp = DateTime.Now
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMessage))
            {
                return RedirectToPage();
            }

            try
            {
                IsProcessing = true;
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                // Create a new chat if one isn't already loaded
                videoscriptAI.Data.Chat chat;
                if (CurrentChatId == 0)
                {
                    // Extract a title from the first user message
                    var title = NewMessage.Length > 30
                        ? NewMessage.Substring(0, 30) + "..."
                        : NewMessage;

                    chat = new videoscriptAI.Data.Chat
                    {
                        Title = title,
                        UserId = user.Id,
                        CreatedAt = DateTime.Now
                    };

                    _dbContext.Chats.Add(chat);
                    await _dbContext.SaveChangesAsync();
                    CurrentChatId = chat.Id;
                }
                else
                {
                    chat = await _dbContext.Chats
                        .Include(c => c.ChatContents)
                        .FirstOrDefaultAsync(c => c.Id == CurrentChatId);

                    if (chat == null || chat.UserId != user.Id)
                    {
                        return NotFound();
                    }
                }

                // Load existing chat messages for context
                ChatMessages = chat.ChatContents
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessageViewModel
                    {
                        Text = m.Content,
                        IsUser = m.IsFromUser,
                        Timestamp = m.CreatedAt
                    })
                    .ToList();

                // Add and save user message
                var userMessage = new ChatMessageViewModel
                {
                    Text = NewMessage,
                    IsUser = true,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(userMessage);

                var userChatContent = new ChatContent
                {
                    Content = NewMessage,
                    IsFromUser = true,
                    CreatedAt = DateTime.Now,
                    ChatId = chat.Id
                };
                _dbContext.ChatContents.Add(userChatContent);

                // Get AI response
                _logger.LogInformation("Getting response from Gemini API");
                var response = await _geminiService.GetResponseAsync(ChatMessages);

                // Add and save AI response
                var aiMessage = new ChatMessageViewModel
                {
                    Text = response,
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(aiMessage);

                var aiChatContent = new ChatContent
                {
                    Content = response,
                    IsFromUser = false,
                    CreatedAt = DateTime.Now,
                    ChatId = chat.Id
                };
                _dbContext.ChatContents.Add(aiChatContent);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Chat messages saved to database");

                NewMessage = string.Empty;
                IsProcessing = false;

                // If it's an AJAX request, return JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new
                    {
                        success = true,
                        userMessage = userMessage,
                        aiMessage = aiMessage,
                        chatId = chat.Id
                    });
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, error = ex.Message });
                }

                ChatMessages.Add(new ChatMessageViewModel
                {
                    Text = "Sorry, I encountered an error. Please try again.",
                    IsUser = false,
                    Timestamp = DateTime.Now
                });

                IsProcessing = false;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostNewChatAsync()
        {
            return RedirectToPage();
        }
    }

    public class ChatMessageViewModel
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }

        public string FormattedText => GetFormattedText();

        private string GetFormattedText()
        {
            if (string.IsNullOrEmpty(Text)) return string.Empty;

            // Convert line breaks to HTML breaks
            var formatted = Text.Replace("\n", "<br>");

            // Format bullet points
            formatted = System.Text.RegularExpressions.Regex.Replace(
                formatted,
                @"•\s+(.*?)(?=<br>|$)",
                "<span style=\"display:flex\"><span style=\"margin-right:8px\">•</span><span>$1</span></span>");

            return formatted;
        }
    }

    public class ChatHistoryItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}