using System;
using System;
using System.Collections.Generic;

namespace videoscriptAI.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ContentType { get; set; } // e.g., "Video", "YouTube Link", etc.

        public ICollection<ChatContent> ChatContents { get; set; }
    }
}