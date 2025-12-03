using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DruzabniDogodki.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Uporabniško ime je obvezno.")]
            [Display(Name = "Uporabniško ime")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Geslo je obvezno.")]
            [DataType(DataType.Password)]
            [Display(Name = "Geslo")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Zapomni si me")]
            public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public IActionResult OnPost(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: Replace with real authentication logic (Identity, custom auth, etc.)
            var isValidUser = string.Equals(Input.UserName, "demo", System.StringComparison.OrdinalIgnoreCase)
                              && Input.Password == "demo";

            if (!isValidUser)
            {
                ModelState.AddModelError(string.Empty, "Nepravilno uporabniško ime ali geslo.");
                return Page();
            }

            // Placeholder: on successful login redirect to ReturnUrl
            return LocalRedirect(ReturnUrl!);
        }
    }
}
