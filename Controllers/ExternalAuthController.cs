using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using videoscriptAI.Models;

namespace videoscriptAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalAuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExternalAuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet("GoogleLogin")]
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", Url.Action("GoogleCallback", "ExternalAuth", new { returnUrl }));
            return Challenge(properties, "Google");
        }

        [HttpGet("GoogleCallback")]
        public async Task<IActionResult> GoogleCallback(string returnUrl = "/", string remoteError = null)
        {
            if (remoteError != null)
            {
                return Redirect($"/Error?message={remoteError}");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            // Estrai l'email dall'info
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                return Redirect("/Error?message=Email non trovata nel provider esterno");
            }

            // Controllo se ci sia già un account con questa email
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                // L'utente esiste già, controlla se ha già questo login esterno
                var existingLogins = await _userManager.GetLoginsAsync(user);
                var existingLogin = existingLogins.FirstOrDefault(l =>
                    l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey);

                if (existingLogin == null)
                {
                    // L'account esiste ma non ha questo login esterno collegato, aggiungiamo l'associazione
                    var addLoginResult = await _userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                    {
                        return Redirect("/Error?message=Impossibile aggiungere il login esterno");
                    }
                }

                // Effettua il login con l'account esistente
                var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider,
                                                                             info.ProviderKey,
                                                                             isPersistent: false,
                                                                             bypassTwoFactor: true);

                if (signInResult.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    // Accedi direttamente con l'utente (bypass di eventuali restrizioni)
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }
            else
            {
                // Crea un nuovo utente con i dati di Google
                var newUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true // L'email è già verificata da Google
                };

                var createResult = await _userManager.CreateAsync(newUser);
                if (createResult.Succeeded)
                {
                    var addLoginResult = await _userManager.AddLoginAsync(newUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(newUser, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                // Gestione errori
                return Redirect("/Error?message=Impossibile creare un nuovo utente");
            }
        }
    }
}