using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using videoscriptAI.Models;

namespace videoscriptAI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public bool IsAdmin { get; set; }

        public ICollection<Chat> Chats { get; set; }
    }
}