using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;

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

            // Povezava na SQLite bazo (db datoteka je v rootu projekta)
            const string connectionString = "Data Source=druzabnidogodki.db";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // 1) preveri, ali uporabniško ime že obstaja
            const string checkSql = "SELECT COUNT(*) FROM Users WHERE UserName = $u";

            using (var checkCmd = new SqliteCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("$u", Input.UserName);
                var count = (long)(checkCmd.ExecuteScalar() ?? 0);

                if (count > 0)
                {
                    ModelState.AddModelError(string.Empty, "Uporabniško ime je že zasedeno.");
                    return Page();
                }
            }

            // 2) vstavi novega uporabnika
            const string insertSql =
                "INSERT INTO Users (UserName, Email, Password) VALUES ($u, $e, $p)";

            using (var insertCmd = new SqliteCommand(insertSql, connection))
            {
                insertCmd.Parameters.AddWithValue("$u", Input.UserName);
                insertCmd.Parameters.AddWithValue("$e", Input.Email);
                insertCmd.Parameters.AddWithValue("$p", Input.Password); // v realnosti: hash!

                insertCmd.ExecuteNonQuery();
            }

            // 3) po uspešni registraciji na login
            return RedirectToPage("/Login");
        }
    }
}
