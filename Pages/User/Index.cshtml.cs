using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace videoscriptAI.Pages.User
{
    [Authorize(Roles = "User")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}