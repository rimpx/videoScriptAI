using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using videoscriptAI.Data;
using videoscriptAI.Models;

namespace videoscriptAI.Pages.Admin
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int TotalChats { get; set; }
        public int ActiveUsers { get; set; }
        public List<UserViewModel> RecentUsers { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verifica se l'utente è amministratore
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsAdmin)
            {
                return RedirectToPage("/Index");
            }

            // Ottieni statistiche
            TotalUsers = await _context.Users.CountAsync();
            TotalChats = await _context.Chats.CountAsync();
            ActiveUsers = await _context.Users.CountAsync(u => u.Chats.Any());

            // Ottieni ultimi 5 utenti registrati
            RecentUsers = await _context.Users
                .OrderByDescending(u => u.Id) // Sostituisci con un campo di data se disponibile
                .Take(5)
                .Select(u => new UserViewModel
                {
                    
                    Email = u.Email,
                    RegisterDate = DateTime.Now, // Sostituisci con data effettiva se disponibile
                    IsAdmin = u.IsAdmin
                })
                .ToListAsync();

            return Page();
        }

        public class UserViewModel
        {
            
            public string Email { get; set; }
            public DateTime RegisterDate { get; set; }
            public bool IsAdmin { get; set; }
        }
    }
}