using System;

namespace videoscriptAI.Data
{
    public class ChatContent
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Add these to complete the relationship
        public int ChatId { get; set; }
        public Chat Chat { get; set; }
    }
}