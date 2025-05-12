using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using videoscriptAI.Models;

namespace videoscriptAI.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAdmin(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            const string adminEmail = "admin@videoscriptai.com";
            const string adminPassword = "Admin@123456"; // Cambia con una password più sicura

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                Console.WriteLine($"Creazione dell'utente amministratore: {adminEmail}");

                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    IsAdmin = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    Console.WriteLine("Utente amministratore creato con successo!");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Errore nella creazione dell'utente amministratore: {errors}");
                }
            }
        }
    }
}