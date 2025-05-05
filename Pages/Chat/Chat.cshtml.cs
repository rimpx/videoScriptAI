using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace videoscriptAI.Pages
{
    public class ChatModel : PageModel
    {
        private readonly ILogger<ChatModel> _logger;
        private readonly string _sessionFilePath;

        // Service to handle AI interactions - you would inject your actual service here
        // private readonly IAIService _aiService;

        [BindProperty]
        public string NewMessage { get; set; }

        public List<ChatMessage> ChatMessages { get; private set; } = new List<ChatMessage>();

        public bool IsProcessing { get; private set; } = false;

        // Session ID to persist chat history between page refreshes
        private string SessionId => HttpContext.Session.Id;

        public ChatModel(ILogger<ChatModel> logger)
        {
            _logger = logger;

            // Define path for session storage
            var tempPath = Path.GetTempPath();
            _sessionFilePath = Path.Combine(tempPath, "videoScriptAI", "sessions");

            // Ensure directory exists
            if (!Directory.Exists(_sessionFilePath))
            {
                Directory.CreateDirectory(_sessionFilePath);
            }
        }

        public void OnGet()
        {
            // Load chat history for this session
            LoadChatHistory();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMessage))
            {
                return RedirectToPage();
            }

            try
            {
                // Load existing chat history
                LoadChatHistory();

                // Add user message
                var userMessage = new ChatMessage
                {
                    Text = NewMessage,
                    IsUser = true,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(userMessage);
                SaveChatHistory();

                // Process with AI
                IsProcessing = true;
                var response = await GenerateAIResponse(NewMessage);

                // Add AI response
                var aiMessage = new ChatMessage
                {
                    Text = response,
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(aiMessage);
                SaveChatHistory();

                IsProcessing = false;
                NewMessage = string.Empty;

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                ModelState.AddModelError(string.Empty, "An error occurred while processing your message. Please try again.");
                return Page();
            }
        }

        private async Task<string> GenerateAIResponse(string userMessage)
        {
            // TODO: Replace with actual AI service integration
            // In a real implementation, you would call your AI service here
            // return await _aiService.GenerateResponseAsync(userMessage, ChatMessages);

            // Simulate AI thinking time
            await Task.Delay(1000);

            // Demo responses based on keywords
            if (userMessage.Contains("script", StringComparison.OrdinalIgnoreCase))
            {
                return "I'd be happy to help you with your script! Could you tell me more about the video topic, target audience, and approximate length you're aiming for?";
            }
            else if (userMessage.Contains("outline", StringComparison.OrdinalIgnoreCase))
            {
                return "Creating a solid outline is a great first step! Here's a basic structure you could follow:\n\n1. Hook/Attention grabber (15 sec)\n2. Introduction and topic overview (30-45 sec)\n3. Main points (3-5 sections, 1-2 min each)\n4. Practical examples or demonstrations\n5. Summary of key takeaways\n6. Call to action\n\nWould you like me to customize this outline for your specific topic?";
            }
            else if (userMessage.Contains("intro", StringComparison.OrdinalIgnoreCase))
            {
                return "For a compelling introduction, consider these approaches:\n\n- Ask a thought-provoking question\n- Share a surprising statistic\n- Tell a short, relevant story\n- Make a bold statement\n- Create a \"what if\" scenario\n\nWhich of these would work best for your video topic?";
            }
            else
            {
                return "I'm your AI assistant for video script creation. I can help with outlining, writing, refining, and optimizing your scripts. What specific aspect of your video script would you like help with today?";
            }
        }

        private void LoadChatHistory()
        {
            var filePath = Path.Combine(_sessionFilePath, $"{SessionId}.json");

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    ChatMessages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading chat history");
                    ChatMessages = new List<ChatMessage>();
                }
            }
            else
            {
                ChatMessages = new List<ChatMessage>();
            }
        }

        private void SaveChatHistory()
        {
            var filePath = Path.Combine(_sessionFilePath, $"{SessionId}.json");

            try
            {
                var json = JsonSerializer.Serialize(ChatMessages);
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat history");
            }
        }

        public class ChatMessage
        {
            public string Text { get; set; }
            public bool IsUser { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}