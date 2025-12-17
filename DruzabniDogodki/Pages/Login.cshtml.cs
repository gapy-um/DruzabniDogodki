using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using System;

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
                return Page();

            // HARDCODE admin/admin
            if (Input.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase) && Input.Password == "admin")
            {
                HttpContext.Session.SetString("UserName", "admin");
                HttpContext.Session.SetString("Role", "Admin");
                return LocalRedirect(ReturnUrl!);
            }

            const string connectionString = "Data Source=druzabnidogodki.db";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            const string sql = @"
SELECT UserName, Role
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

            var username = reader.GetString(0);
            var role = reader.IsDBNull(1) ? "User" : reader.GetString(1);

            HttpContext.Session.SetString("UserName", username);
            HttpContext.Session.SetString("Role", role);

            return LocalRedirect(ReturnUrl!);
        }
    }
}
