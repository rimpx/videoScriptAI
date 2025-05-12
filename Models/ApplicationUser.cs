using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using videoscriptAI.Data;
using videoscriptAI.Models;

namespace videoscriptAI.Models
{
    public class ApplicationUser : IdentityUser
    {
        
        public bool IsAdmin { get; set; }

        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
    }
}