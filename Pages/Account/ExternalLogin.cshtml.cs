using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace videoscriptAI.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public ExternalLoginModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Properties used in the ExternalLogin.cshtml view
        public bool ShowErrorMessage { get; set; }
        public string ErrorMessage { get; set; }
        public bool ShowForm { get; set; }

        [BindProperty]
        [EmailAddress]
        [Required]
        public string Email { get; set; }

        public async Task<IActionResult> OnGetAsync(string remoteError = null)
        {
            if (remoteError != null)
            {
                ShowErrorMessage = true;
                ErrorMessage = $"Error from external provider: {remoteError}";
                return Page();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ShowErrorMessage = true;
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login");
            }

            var emailClaim = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email);
            if (emailClaim != null)
            {
                Email = emailClaim.Value;
                return RedirectToPage("./ExternalLoginCallback");
            }

            // If no email is provided, show the form
            ShowForm = true;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ShowForm = true;
                return Page();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ShowErrorMessage = true;
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login");
            }

            var user = new IdentityUser { UserName = Email, Email = Email };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ShowForm = true;
                return Page();
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ShowForm = true;
                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToPage("./ExternalLoginCallback");
        }
    }
}