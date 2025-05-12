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
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UsersModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserViewModel> Users { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            // Verifica se l'utente è amministratore
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                return RedirectToPage("/Index");
            }

            CurrentPage = page < 1 ? 1 : page;

            // Ottieni il numero totale di utenti per calcolare la paginazione
            var totalUsers = await _context.Users.CountAsync();
            TotalPages = (int)Math.Ceiling(totalUsers / (double)PageSize);

            // Ottieni tutti gli utenti con paginazione
            var users = await _context.Users
                .OrderBy(u => u.Email)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Users = users.Select(u => new UserViewModel
            {
                Id = u.Id,
               
                Email = u.Email,
                IsAdmin = u.IsAdmin
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostMakeAdminAsync(string userId)
        {
            // Verifica che l'utente corrente sia un amministratore
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                return RedirectToPage("/Index");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsAdmin = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAdminAsync(string userId)
        {
            // Verifica che l'utente corrente sia un amministratore
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                return RedirectToPage("/Index");
            }

            // Controlla che non stia cercando di rimuovere se stesso
            if (currentUser.Id == userId)
            {
                ModelState.AddModelError("", "Non puoi rimuovere i privilegi di amministratore a te stesso.");
                return await OnGetAsync();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsAdmin = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            // Verifica che l'utente corrente sia un amministratore
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                return RedirectToPage("/Index");
            }

            // Controlla che non stia cercando di eliminare se stesso
            if (currentUser.Id == userId)
            {
                ModelState.AddModelError("", "Non puoi eliminare il tuo account da qui.");
                return await OnGetAsync();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Prima elimina tutte le chat dell'utente
                var userChats = await _context.Chats
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                foreach (var chat in userChats)
                {
                    var chatContents = await _context.ChatContents
                        .Where(c => c.ChatId == chat.Id)
                        .ToListAsync();

                    _context.ChatContents.RemoveRange(chatContents);
                }

                _context.Chats.RemoveRange(userChats);

                // Poi elimina l'utente
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public class UserViewModel
        {
            public string Id { get; set; }
          
            public string Email { get; set; }
            public bool IsAdmin { get; set; }
        }
    }
}