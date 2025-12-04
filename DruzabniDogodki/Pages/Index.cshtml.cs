using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;

namespace DruzabniDogodki.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsAdmin { get; set; }

        public void OnGet()
        {
            var isAdminStr = HttpContext.Session.GetString("IsAdmin");
            IsAdmin = bool.TryParse(isAdminStr, out var admin) && admin;
        }
    }
}
