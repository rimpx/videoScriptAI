using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using videoscriptAI.Models;

namespace videoscriptAI.Services
{
    public class GeminiService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model = "gemini-1.0-pro";
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Gemini:ApiKey"];
            _logger = logger;
        }

        public async Task<string> GetResponseAsync(List<ChatMessage> messages)
        {
            try
            {
                // Convert our app's messages format to Gemini's format
                var geminiMessages = ConvertToGeminiMessages(messages);

                // Create request body
                var requestBody = new
                {
                    model = _model,
                    messages = geminiMessages,
                    temperature = 0.7,
                    top_p = 0.95,
                    max_output_tokens = 1024
                };

                // Serialize and prepare request
                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                // Send request to Gemini API
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}",
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

                _logger.LogError($"Gemini API error: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                return "I'm sorry, I couldn't generate a response. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return "I'm sorry, an error occurred. Please try again later.";
            }
        }

        private List<object> ConvertToGeminiMessages(List<ChatMessage> messages)
        {
            var geminiMessages = new List<object>();

            foreach (var message in messages)
            {
                geminiMessages.Add(new
                {
                    role = message.IsFromUser ? "user" : "model",
                    parts = new[] { new { text = message.Content } }
                });
            }

            return geminiMessages;
        }
    }

    // Classes for deserializing the Gemini API response
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
}