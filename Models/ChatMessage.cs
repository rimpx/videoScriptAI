using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using videoscriptAI.Models;

namespace videoscriptAI.Models // Sostituisci con il tuo namespace
{
    public class ChatMessage
    {
        [Key] // Chiave Primaria
        public int Id { get; set; }

        [Required]
        public int ChatId { get; set; } // Foreign Key verso Chat

        [ForeignKey("ChatId")]
        public virtual Chat Chat { get; set; } = null!; // Navigation property (richiesta)

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)] // Es: "VideoFile", "YouTubeLink"
        public string InputType { get; set; } = string.Empty;

        [Required]
        public string InputContent { get; set; } = string.Empty; // URL o riferimento al file

        [Required]
        public string OutputScript { get; set; } = string.Empty; // Script generato da Gemini

        [MaxLength(50)] // Es: "Success", "Processing", "Error"
        public string? GeminiProcessingStatus { get; set; }

        public string? ErrorMessage { get; set; } // Eventuale messaggio di errore
    }
}