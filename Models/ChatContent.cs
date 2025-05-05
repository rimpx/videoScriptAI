using System;

namespace videoscriptAI.Models
{
    public class ChatContent
    {
        public int Id { get; set; }
        public int ChatId { get; set; }
        public Chat Chat { get; set; }
        public string Message { get; set; }
        public string MediaPath { get; set; } // Path to the uploaded video file
        public DateTime SentAt { get; set; }
    }
}