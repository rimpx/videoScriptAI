using System;

namespace videoscriptAI.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ChatMessage(string content, bool isFromUser)
        {
            Content = content;
            IsFromUser = isFromUser;
        }
    }
}