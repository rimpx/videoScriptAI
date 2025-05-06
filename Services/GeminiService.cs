using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        public GeminiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GeminiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["ApiKeys:Gemini"];
            _logger = logger;

            // Verifica che la chiave API sia configurata
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Chiave API Gemini non configurata");
                throw new InvalidOperationException("La chiave API Gemini non è configurata. Aggiungerla in appsettings.json.");
            }
        }

        /// <summary>
        /// Invia una lista di messaggi all'AI e ottiene una risposta
        /// </summary>
        public async Task<string> GetResponseAsync(List<ChatMessage> messages)
        {
            try
            {
                _logger.LogInformation("Invio richiesta all'API Gemini");

                var client = _httpClientFactory.CreateClient();
                var requestUrl = $"{_baseUrl}?key={_apiKey}";

                // Costruisci il messaggio nel formato richiesto da Gemini API
                var contents = new List<object>();

                foreach (var message in messages)
                {
                    var content = new
                    {
                        role = message.IsFromUser ? "user" : "model",
                        parts = new[]
                        {
                            new { text = message.Content }
                        }
                    };

                    contents.Add(content);
                }

                var requestData = new
                {
                    contents = contents
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonContent.Create(requestData, options: jsonOptions);

                _logger.LogInformation("Request data: {RequestData}", JsonSerializer.Serialize(requestData));

                var response = await client.PostAsync(requestUrl, jsonContent);

                // Log della risposta anche in caso di errore
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response status: {Status}, Content: {Content}",
                    response.StatusCode, responseContent);

                response.EnsureSuccessStatusCode();

                var jsonResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, jsonOptions);

                // Estrai la risposta generata dal modello
                string textResponse = jsonResponse?.Candidates?
                    .FirstOrDefault()?.Content?.Parts?
                    .FirstOrDefault()?.Text ?? string.Empty;

                if (string.IsNullOrEmpty(textResponse))
                {
                    _logger.LogWarning("Risposta vuota ricevuta dall'API Gemini");
                    return "Mi dispiace, non sono riuscito a processare la richiesta. Per favore riprova più tardi.";
                }

                _logger.LogInformation("Risposta ricevuta dall'API Gemini");
                return textResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la chiamata all'API Gemini: {Message}", ex.Message);
                return "Si è verificato un errore durante l'elaborazione della richiesta. Dettagli: " + ex.Message;
            }
        }

        /// <summary>
        /// Invia un URL YouTube all'API e ottiene un'analisi
        /// </summary>
        public async Task<string> ProcessYoutubeVideoAsync(string youtubeUrl, string prompt)
        {
            try
            {
                _logger.LogInformation($"Elaborazione video YouTube: {youtubeUrl} con prompt: {prompt}");

                var client = _httpClientFactory.CreateClient();
                var requestUrl = $"{_baseUrl}?key={_apiKey}";

                // Controllo che l'URL sia valido e completo
                if (!youtubeUrl.StartsWith("http"))
                {
                    youtubeUrl = "https://" + youtubeUrl;
                }

                var requestData = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new
                                {
                                    fileData = new
                                    {
                                        fileUri = youtubeUrl
                                    }
                                }
                            }
                        }
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonString = JsonSerializer.Serialize(requestData, jsonOptions);
                _logger.LogInformation("YouTube request data: {RequestData}", jsonString);

                var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUrl, content);

                // Log della risposta anche in caso di errore
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("YouTube response status: {Status}, Content: {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Errore API: {response.StatusCode}, Dettagli: {responseContent}");
                }

                var jsonResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, jsonOptions);

                // Estrai la risposta generata dal modello
                string textResponse = jsonResponse?.Candidates?
                    .FirstOrDefault()?.Content?.Parts?
                    .FirstOrDefault()?.Text ?? string.Empty;

                if (string.IsNullOrEmpty(textResponse))
                {
                    _logger.LogWarning("Risposta vuota ricevuta dall'API Gemini per YouTube");
                    return "Mi dispiace, non sono riuscito ad analizzare il video. Potrebbe essere troppo lungo o non accessibile. Per favore riprova con un altro video.";
                }

                _logger.LogInformation("Risposta ricevuta dall'API Gemini per il video YouTube");
                return textResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del video YouTube: {Message}", ex.Message);
                return "Si è verificato un errore durante l'analisi del video. Dettagli: " + ex.Message;
            }
        }

        /// <summary>
        /// Invia un file video all'API Gemini e ottiene un'analisi
        /// </summary>
        public async Task<string> ProcessVideoFileAsync(string filePath, string prompt)
        {
            try
            {
                _logger.LogInformation($"Elaborazione file video: {filePath}");

                // Per file piccoli (< 20MB), possiamo inviare direttamente come base64
                if (new FileInfo(filePath).Length < 20 * 1024 * 1024)
                {
                    return await ProcessSmallVideoFileAsync(filePath, prompt);
                }
                // Per file più grandi, per ora restituiamo un errore gestito
                else
                {
                    return "I file video superiori a 20MB non sono supportati in questa versione. " +
                           "Per analizzare video più grandi, carica il video su YouTube e usa l'URL.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'elaborazione del file video: {Message}", ex.Message);
                return "Si è verificato un errore durante l'elaborazione del file video. Dettagli: " + ex.Message;
            }
        }

        private async Task<string> ProcessSmallVideoFileAsync(string filePath, string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{_baseUrl}?key={_apiKey}";

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            string base64Video = Convert.ToBase64String(fileBytes);

            var requestData = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new
                            {
                                inlineData = new
                                {
                                    mimeType = "video/mp4",
                                    data = base64Video
                                }
                            }
                        }
                    }
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(requestData, jsonOptions);
            _logger.LogInformation("File video request data length: {Length}", jsonString.Length);

            var content = new StringContent(jsonString, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(requestUrl, content);

            // Log della risposta anche in caso di errore
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("File video response status: {Status}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Errore API: {response.StatusCode}, Dettagli: {responseContent}");
            }

            var jsonResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, jsonOptions);

            return jsonResponse?.Candidates?
                .FirstOrDefault()?.Content?.Parts?
                .FirstOrDefault()?.Text ?? "Non sono riuscito ad analizzare il video.";
        }

        // Classe per deserializzare la risposta di Gemini
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