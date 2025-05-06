using System;

namespace videoscriptAI.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Costruttore esistente
        public ChatMessage(string content, bool isFromUser)
        {
            Content = content;
            IsFromUser = isFromUser;
        }

        // Aggiungi questo costruttore senza parametri
        public ChatMessage()
        {
            // Costruttore vuoto per permettere l'inizializzazione di oggetti
        }
    }
}