using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using videoscriptAI.Pages;

namespace videoscriptAI.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["Gemini:ApiKey"];
            _logger = logger;
        }

        public async Task<string> GetResponseAsync(List<ChatMessageViewModel> messages)
        {
            try
            {
                // Convert our chat messages to Gemini format
                var geminiMessages = new List<object>();

                // Add system prompt as the first message
                geminiMessages.Add(new
                {
                    role = "system",
                    parts = new List<object>
                    {
                        new { text = "You are a helpful Video Script Assistant specialized in helping users create professional video scripts. Be concise, creative and professional." }
                    }
                });

                // Add conversation history
                foreach (var message in messages)
                {
                    geminiMessages.Add(new
                    {
                        role = message.IsUser ? "user" : "model",
                        parts = new List<object>
                        {
                            new { text = message.Text }
                        }
                    });
                }

                var requestBody = new
                {
                    contents = geminiMessages,
                    generationConfig = new
                    {
                        temperature = 0.7,
                        max_output_tokens = 2048,
                        top_p = 0.95,
                        top_k = 40
                    }
                };

                _logger.LogInformation("Sending request to Gemini API with {MessageCount} messages", messages.Count);

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                // Use the correct endpoint format for Gemini Pro chat model
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1/models/gemini-1.0-pro:generateContent?key={_apiKey}",
                    content);

                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Received response from Gemini API. Status code: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseJson);
                    return "Sorry, I encountered an error. Please try again.";
                }

                // Parse the response
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

                if (geminiResponse?.candidates == null || geminiResponse.candidates.Count == 0 ||
                    geminiResponse.candidates[0].content == null ||
                    geminiResponse.candidates[0].content.parts == null ||
                    geminiResponse.candidates[0].content.parts.Count == 0)
                {
                    _logger.LogError("Invalid Gemini API response format");
                    return "Sorry, I received an invalid response format. Please try again.";
                }

                return geminiResponse.candidates[0].content.parts[0].text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return "Sorry, I encountered an error. Please try again.";
            }
        }
    }

    public class GeminiResponse
    {
        public List<GeminiCandidate> candidates { get; set; }
        public PromptFeedback promptFeedback { get; set; }
    }

    public class GeminiCandidate
    {
        public GeminiContent content { get; set; }
        public string finishReason { get; set; }
        public int index { get; set; }
    }

    public class GeminiContent
    {
        public List<GeminiPart> parts { get; set; }
        public string role { get; set; }
    }

    public class GeminiPart
    {
        public string text { get; set; }
    }

    public class PromptFeedback
    {
        public SafetyRating[] safetyRatings { get; set; }
    }

    public class SafetyRating
    {
        public string category { get; set; }
        public string probability { get; set; }
    }
}