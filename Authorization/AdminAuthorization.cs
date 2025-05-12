using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using videoscriptAI.Models;
using System.Threading.Tasks;

namespace videoscriptAI.Authorization
{
    // Requisito di autorizzazione per gli admin
    public class AdminRoleRequirement : IAuthorizationRequirement
    {
        // Non ha bisogno di proprietà o costruttori
    }

    // Handler che verifica la proprietà IsAdmin dell'utente
    public class AdminRoleHandler : AuthorizationHandler<AdminRoleRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminRoleHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminRoleRequirement requirement)
        {
            // Verifica se l'utente è autenticato
            if (!context.User.Identity.IsAuthenticated)
            {
                return; // Non soddisfa il requisito
            }

            // Ottieni l'utente dal database per verificare la proprietà IsAdmin
            var user = await _userManager.GetUserAsync(context.User);

            if (user != null && user.IsAdmin)
            {
                context.Succeed(requirement);
            }
        }
    }
}