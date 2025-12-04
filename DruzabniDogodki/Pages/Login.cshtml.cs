using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;


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

            const string connectionString = "Data Source=druzabnidogodki.db";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            const string sql = @"
        SELECT UserName, IsAdmin
        FROM Users
        WHERE UserName = $u AND Password = $p
        LIMIT 1;";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("$u", Input.UserName);
            cmd.Parameters.AddWithValue("$p", Input.Password);

            using var reader = cmd.ExecuteReader();
            
            if (!reader.Read())
            {
                ModelState.AddModelError(string.Empty, "Nepravilno uporabniško ime ali geslo.");
                return Page();
            }

            var userName = reader.GetString(0);
            var isAdmin = reader.IsDBNull(1) ? false : reader.GetBoolean(1);

            // ? prijava uspela – zapišemo v Session
            HttpContext.Session.SetString("UserName", userName);
            HttpContext.Session.SetString("IsAdmin", isAdmin.ToString());

            // redirect na Index (ali ReturnUrl)
            return LocalRedirect(ReturnUrl!);
        }

    }
}


