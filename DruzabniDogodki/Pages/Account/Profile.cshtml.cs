using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;

namespace DruzabniDogodki.Pages.Account
{
    public class ProfileModel : PageModel
    {
        [BindProperty, Required]
        public string Username { get; set; } = "";

        [BindProperty, Required, EmailAddress]
        public string Email { get; set; } = "";

        public string Message { get; set; } = "";

        const string connectionString = "Data Source=druzabnidogodki.db";

        public IActionResult OnGet()
        {
            var sessionUser = HttpContext.Session.GetString("UserName");
            if (sessionUser == null)
                return RedirectToPage("/Login");

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            const string sql = @"
                SELECT UserName, Email
                FROM Users
                WHERE UserName = $u
                LIMIT 1;";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("$u", sessionUser);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                Username = reader["UserName"]!.ToString()!;
                Email = reader["Email"]!.ToString()!;
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var sessionUser = HttpContext.Session.GetString("UserName");
            if (sessionUser == null)
                return RedirectToPage("/Login");

            if (!ModelState.IsValid)
                return Page();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            const string sql = @"
                UPDATE Users
                SET UserName = $newU, Email = $e
                WHERE UserName = $oldU;";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("$newU", Username);
            cmd.Parameters.AddWithValue("$e", Email);
            cmd.Parameters.AddWithValue("$oldU", sessionUser);

            cmd.ExecuteNonQuery();

            // posodobi session če se je username spremenil
            HttpContext.Session.SetString("UserName", Username);

            Message = "Profil je uspešno posodobljen.";
            return Page();
        }
    }
}
