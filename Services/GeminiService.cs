using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using videoscriptAI.Models;

namespace videoscriptAI.Services
{
    public class GeminiService : IAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

        public GeminiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GeminiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["ApiKeys:Gemini"];
            _logger = logger;
        }

        /// <summary>
        /// Implementazione dell'interfaccia IAIService per inviare messaggi all'API Gemini e ricevere risposte
        /// </summary>
        public async Task<string> GetResponseAsync(List<ChatMessage> messages)
        {
            try
            {
                _logger.LogInformation("Invio richiesta all'API Gemini");

                var client = _httpClientFactory.CreateClient();
                var requestUrl = $"{_baseUrl}?key={_apiKey}";

                // Converti i messaggi nel formato richiesto da Gemini API
                // Mappa IsFromUser a "user" o "assistant" come richiesto da Gemini
                var requestData = new
                {
                    contents = messages.Select(m => new
                    {
                        role = m.IsFromUser ? "user" : "assistant", // Trasformazione da IsFromUser a role
                        parts = new[] { new { text = m.Content } }
                    }).ToArray()
                };

                var response = await client.PostAsJsonAsync(requestUrl, requestData);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();

                // Estrai la risposta generata dal modello
                string textResponse = jsonResponse?.Candidates?
                    .FirstOrDefault()?.Content?.Parts?
                    .FirstOrDefault()?.Text ?? string.Empty;

                _logger.LogInformation("Risposta ricevuta dall'API Gemini");
                return textResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata all'API Gemini");
                throw;
            }
        }

        // Classe interna per deserializzare la risposta di Gemini
        private class GeminiResponse
        {
            public List<Candidate> Candidates { get; set; }

            public class Candidate
            {
                public Content Content { get; set; }
            }

            public class Content
            {
                public List<Part> Parts { get; set; }
            }

            public class Part
            {
                public string Text { get; set; }
            }
        }
    }
}