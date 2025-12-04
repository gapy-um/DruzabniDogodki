using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;

namespace DruzabniDogodki.Pages
{
    public class ProfileModel : PageModel
    {
        [BindProperty]
        public UserModel User { get; set; } = new();

        public string Message { get; set; } = "";

        public class UserModel
        {
            public int Id { get; set; }

            [Required]
            public string UserName { get; set; } = "";

            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            public string Password { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            const string cs = "Data Source=druzabnidogodki.db";

            using var con = new SqliteConnection(cs);
            con.Open();

            var sql = "SELECT Id, UserName, Email, Password FROM Users WHERE UserName = $u LIMIT 1";

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("$u", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                User.Id = reader.GetInt32(0);
                User.UserName = reader.GetString(1);
                User.Email = reader.GetString(2);
                User.Password = reader.GetString(3);
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            const string cs = "Data Source=druzabnidogodki.db";

            using var con = new SqliteConnection(cs);
            con.Open();

            var sql = @"UPDATE Users 
                        SET UserName = $u, Email = $e, Password = $p
                        WHERE Id = $id";

            using var cmd = new SqliteCommand(sql, con);
            cmd.Parameters.AddWithValue("$u", User.UserName);
            cmd.Parameters.AddWithValue("$e", User.Email);
            cmd.Parameters.AddWithValue("$p", User.Password);
            cmd.Parameters.AddWithValue("$id", User.Id);

            cmd.ExecuteNonQuery();

            // Po spremembi posodobimo Session username, èe ga uporabnik spremeni
            HttpContext.Session.SetString("UserName", User.UserName);

            Message = "Profil uspešno posodobljen!";

            return Page();
        }

        public IActionResult OnPostDelete()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (username == null)
                return RedirectToPage("/Login");

            const string cs = "Data Source=druzabnidogodki.db";

            using var con = new SqliteConnection(cs);
            con.Open();

            var cmd = new SqliteCommand("DELETE FROM Users WHERE UserName = $u", con);
            cmd.Parameters.AddWithValue("$u", username);
            cmd.ExecuteNonQuery();

            // Odjava po brisanju
            HttpContext.Session.Clear();

            return RedirectToPage("/Index");
        }
    }
}
