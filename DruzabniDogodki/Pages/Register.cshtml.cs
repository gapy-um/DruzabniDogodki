using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DruzabniDogodki.Pages
{
    public class RegisterModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Uporabniško ime je obvezno.")]
            [Display(Name = "Uporabniško ime")]
            public string UserName { get; set; } = string.Empty;

            [Required(ErrorMessage = "E-pošta je obvezna.")]
            [EmailAddress(ErrorMessage = "Vnesite veljaven e-poštni naslov.")]
            [Display(Name = "E-pošta")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Geslo je obvezno.")]
            [DataType(DataType.Password)]
            [Display(Name = "Geslo")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Potrditev gesla je obvezna.")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Gesli se ne ujemata.")]
            [Display(Name = "Potrdi geslo")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: tukaj pride prava registracija – Identity / DB insert

            // Zaenkrat samo redirect na login
            return RedirectToPage("/Login");
        }
    }
}
