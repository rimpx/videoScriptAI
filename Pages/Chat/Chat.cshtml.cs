using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace videoscriptAI.Pages
{
    public class ChatModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly string _model = "gemini-1.0-pro";
        private const string SessionKeyChatHistory = "_ChatHistory";

        [BindProperty]
        public string NewMessage { get; set; }

        public List<ChatMessageViewModel> ChatMessages { get; private set; } = new List<ChatMessageViewModel>();
        public bool IsProcessing { get; private set; } = false;

        public ChatModel(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _apiKey = _configuration["Gemini:ApiKey"];
        }

        public void OnGet()
        {
            ChatMessages = HttpContext.Session.Get<List<ChatMessageViewModel>>(SessionKeyChatHistory) ??
                new List<ChatMessageViewModel>();
        }

        public async Task<IActionResult> OnPostSendMessageAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewMessage))
                    return BadRequest("Message cannot be empty");

                IsProcessing = true;

                // Get existing chat history or create new one
                ChatMessages = HttpContext.Session.Get<List<ChatMessageViewModel>>(SessionKeyChatHistory) ??
                    new List<ChatMessageViewModel>();

                // Add user message to chat
                var userMessage = new ChatMessageViewModel
                {
                    Text = NewMessage,
                    IsUser = true,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(userMessage);

                // Get AI response
                var response = await GetGeminiResponseAsync(ChatMessages);

                // Add AI response to chat
                var aiMessage = new ChatMessageViewModel
                {
                    Text = response,
                    IsUser = false,
                    Timestamp = DateTime.Now
                };
                ChatMessages.Add(aiMessage);

                // Save chat history to session
                HttpContext.Session.Set(SessionKeyChatHistory, ChatMessages);

                // For AJAX requests
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = true, response = response });
                }

                // Clear input field
                NewMessage = string.Empty;
                IsProcessing = false;

                return Page();
            }
            catch (Exception ex)
            {
                // Log the error (add proper logging here)
                Console.WriteLine($"Error: {ex.Message}");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return new JsonResult(new { success = false, error = "An error occurred" });
                }

                // Add error message for user
                ChatMessages.Add(new ChatMessageViewModel
                {
                    Text = "Sorry, I encountered an error. Please try again.",
                    IsUser = false,
                    Timestamp = DateTime.Now
                });

                HttpContext.Session.Set(SessionKeyChatHistory, ChatMessages);
                IsProcessing = false;
                return Page();
            }
        }

        private async Task<string> GetGeminiResponseAsync(List<ChatMessageViewModel> chatHistory)
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Convert chat history to Gemini format
            var geminiMessages = new List<object>();

            // Add system message if it's a new conversation
            if (chatHistory.Count <= 1)
            {
                geminiMessages.Add(new
                {
                    role = "system",
                    parts = new[] { new { text = "You are a helpful Video Script Assistant. Your primary focus is to help users create, refine, and optimize video scripts. Offer creative suggestions for scripts, help with structure, provide engaging introductions and conclusions, and optimize for different platforms. Be concise yet helpful, professional and creative." } }
                });
            }

            // Add user and model messages
            foreach (var message in chatHistory)
            {
                geminiMessages.Add(new
                {
                    role = message.IsUser ? "user" : "model",
                    parts = new[] { new { text = message.Text } }
                });
            }

            // Create request body
            var requestBody = new
            {
                model = _model,
                messages = geminiMessages,
                temperature = 0.7,
                maxOutputTokens = 1024
            };

            // Serialize and prepare request
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            // Send request to Gemini API
            var response = await httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1/models/{_model}:generateContent?key={_apiKey}",
                content);

            // Process response
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<GeminiResponse>(responseString);

                if (responseObject?.candidates?.Count > 0 &&
                    responseObject.candidates[0]?.content?.parts?.Count > 0)
                {
                    return responseObject.candidates[0].content.parts[0].text;
                }
            }

            // Log the error
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Gemini API error: {response.StatusCode}, {errorContent}");

            return "I'm sorry, I couldn't generate a response. Please try again later.";
        }
    }

    public class ChatMessageViewModel
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string FormattedText
        {
            get
            {
                if (string.IsNullOrEmpty(Text))
                    return string.Empty;

                // Convert new lines to <br>
                var text = Text.Replace("\n", "<br>");

                // Format bullet points
                text = Regex.Replace(text, @"•\s+(.*?)(?=<br>|$)",
                                    "<span style=\"display:flex;\"><span style=\"margin-right:5px;\">•</span><span>$1</span></span>");

                // Bold for ** text **
                text = Regex.Replace(text, @"\*\*(.*?)\*\*", "<strong>$1</strong>");

                // Italic for * text *
                text = Regex.Replace(text, @"\*([^*]+)\*", "<em>$1</em>");

                return text;
            }
        }
    }

    // Gemini API response classes
    public class GeminiResponse
    {
        public List<Candidate> candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
    }

    public class Content
    {
        public List<Part> parts { get; set; }
    }

    public class Part
    {
        public string text { get; set; }
    }

    // Session extensions
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}