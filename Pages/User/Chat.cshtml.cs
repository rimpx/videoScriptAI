using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace videoscriptAI.Pages
{
    public class ChatModel : PageModel
    {
        public List<ChatMessage> Messages { get; set; } = new();
        public List<PreviousChat> PreviousChats { get; set; } = new();

        [BindProperty]
        public string NewMessage { get; set; }

        public void OnGet(int? id)
        {
            // Simula il caricamento dei messaggi da un database o API
            Messages = new List<ChatMessage>
            {
                new ChatMessage { Author = "Gemini", Text = "Ciao! Come posso aiutarti?" },
                new ChatMessage { Author = "Utente", Text = "Sto caricando un video..." }
            };

            // Simula il caricamento delle chat precedenti
            PreviousChats = new List<PreviousChat>
            {
                new PreviousChat { Id = 1, Title = "Chat con video.mp4" },
                new PreviousChat { Id = 2, Title = "Chat con link YouTube" }
            };
        }

        public IActionResult OnPost()
        {
            if (!string.IsNullOrEmpty(NewMessage))
            {
                // Simula l'invio del messaggio
                Messages.Add(new ChatMessage { Author = "Utente", Text = NewMessage });
            }

            return Page();
        }
    }

    public class ChatMessage
    {
        public string Author { get; set; }
        public string Text { get; set; }
    }

    public class PreviousChat
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}