using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using videoscriptAI.Services;
using videoscriptAI.Services;

namespace videoscriptAI.Models
{
    public class Chat
    {
        private readonly ILogger<Chat> _logger;
        private readonly IAIService _aiService;

        [BindProperty]
        public string NewMessage { get; set; }

        public List<ChatMessage> ChatMessages { get; private set; } = new List<ChatMessage>();

        public bool IsProcessing { get; private set; } = false;

        // Generate unique client ID instead of using session
        private string ClientId => Guid.NewGuid().ToString();

        public Chat(ILogger<Chat> logger, IAIService aiService)
        {
            _logger = logger;
            _aiService = aiService;

            // Add initial AI greeting when the chat starts
            var initialMessage = new ChatMessage("Hello! I'm your Video Script AI Assistant. How can I help you today? You can ask me to:\n\n" +
                "• Generate a script outline for your video\n" +
                "• Suggest improvements to your existing script\n" +
                "• Create engaging introductions or conclusions\n" +
                "• Optimize your script for specific platforms", false);

            ChatMessages.Add(initialMessage);
        }

        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMessage))
                return;

            try
            {
                IsProcessing = true;

                // Add user message to chat
                var userMessage = new ChatMessage(NewMessage, true);
                ChatMessages.Add(userMessage);

                // Clear input field
                var messageToProcess = NewMessage;
                NewMessage = string.Empty;

                // Get AI response
                var response = await _aiService.GetResponseAsync(ChatMessages);

                // Add AI response to chat
                var aiMessage = new ChatMessage(response, false);
                ChatMessages.Add(aiMessage);

                // Save chat history
                await SaveChatHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                ChatMessages.Add(new ChatMessage("Sorry, I encountered an error. Please try again later.", false));
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task SaveChatHistoryAsync()
        {
            try
            {
                var tempPath = Path.GetTempPath();
                var sessionFilePath = Path.Combine(tempPath, "videoscriptai", $"chat_{ClientId}.json");

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(sessionFilePath));

                // Save chat messages to file
                await File.WriteAllTextAsync(sessionFilePath,
                    System.Text.Json.JsonSerializer.Serialize(ChatMessages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat history");
            }
        }
    }
}