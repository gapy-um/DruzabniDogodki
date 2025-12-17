using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DruzabniDogodki.Pages.Admin
{
    public class UsersModel : PageModel
    {
        public List<UserItem> Users { get; set; } = new();

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public class UserItem
        {
            public int Id { get; set; }
            public string UserName { get; set; } = "";
            public string? Email { get; set; }
            public string Role { get; set; } = "User";
        }

        public IActionResult OnGet()
        {
            if (!IsAdmin())
                return RedirectToPage("/Index");

            LoadUsers();
            return Page();
        }

        // ✅ BRISANJE UPORABNIKA
        public IActionResult OnPostDelete(int id)
        {
            if (!IsAdmin())
                return RedirectToPage("/Index");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // 1️⃣ preveri uporabnika
            string username;
            using (var check = connection.CreateCommand())
            {
                check.CommandText = "SELECT UserName FROM Users WHERE Id = $id;";
                check.Parameters.AddWithValue("$id", id);
                var res = check.ExecuteScalar();
                if (res == null)
                    return RedirectToPage();

                username = res.ToString()!;
            }

            // ❌ admin ne sme izbrisati samega sebe
            if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToPage();

            using var tx = connection.BeginTransaction();

            // 2️⃣ izbriši rezervacije
            using (var delRes = connection.CreateCommand())
            {
                delRes.CommandText = "DELETE FROM Reservations WHERE UserId = $id;";
                delRes.Parameters.AddWithValue("$id", id);
                delRes.ExecuteNonQuery();
            }

            // 3️⃣ izbriši uporabnika
            using (var delUser = connection.CreateCommand())
            {
                delUser.CommandText = "DELETE FROM Users WHERE Id = $id;";
                delUser.Parameters.AddWithValue("$id", id);
                delUser.ExecuteNonQuery();
            }

            tx.Commit();
            return RedirectToPage();
        }

        private void LoadUsers()
        {
            Users.Clear();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT Id, UserName, Email, Role
FROM Users
ORDER BY CASE WHEN UserName = 'admin' THEN 0 ELSE 1 END, UserName;
";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Users.Add(new UserItem
                {
                    Id = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Role = reader.IsDBNull(3) ? "User" : reader.GetString(3)
                });
            }
        }

        private bool IsAdmin()
        {
            var u = HttpContext.Session.GetString("UserName") ?? "";
            var r = HttpContext.Session.GetString("Role") ?? "";
            return u.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   r.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
