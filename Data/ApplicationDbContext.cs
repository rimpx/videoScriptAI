using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using videoscriptAI.Models;

namespace videoscriptAI.Data
{
    // Aggiungi il tipo generico ApplicationUser qui
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Rimuovi questa riga perché è ridondante quando ApplicationUser è già il tipo utente predefinito
        // public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatContent> ChatContents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customize the ASP.NET Identity model if needed
            builder.Entity<ApplicationUser>().HasMany(c => c.Chats).WithOne(u => u.User).HasForeignKey(c => c.UserId);
            builder.Entity<Chat>().HasMany(c => c.ChatContents).WithOne(cc => cc.Chat).HasForeignKey(cc => cc.ChatId);
        }
    }
}