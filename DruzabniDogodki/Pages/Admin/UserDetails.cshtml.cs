using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DruzabniDogodki.Pages.Admin
{
    public class UserDetailsModel : PageModel
    {
        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string? Email { get; set; }

        public List<ReservationRow> Reservations { get; set; } = new();

        public class ReservationRow
        {
            public int ReservationId { get; set; }
            public int EventId { get; set; }
            public string EventTitle { get; set; } = "";
            public string? Location { get; set; }
            public DateTime EventDate { get; set; }
            public int Quantity { get; set; }
        }

        public IActionResult OnGet(int id)
        {
            if (!IsAdmin())
                return RedirectToPage("/Index");

            UserId = id;

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // user info
            using (var ucmd = connection.CreateCommand())
            {
                ucmd.CommandText = @"SELECT UserName, Email FROM Users WHERE Id = $id LIMIT 1;";
                ucmd.Parameters.AddWithValue("$id", id);

                using var r = ucmd.ExecuteReader();
                if (!r.Read())
                    return RedirectToPage("/Admin/Users");

                UserName = r.GetString(0);
                Email = r.IsDBNull(1) ? null : r.GetString(1);
            }

            // reservations
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
SELECT r.Id, r.EventId, e.Title, e.Location, e.EventDate, r.Quantity
FROM Reservations r
JOIN Events e ON e.Id = r.EventId
WHERE r.UserId = $uid
ORDER BY e.EventDate DESC;
";
                cmd.Parameters.AddWithValue("$uid", id);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Reservations.Add(new ReservationRow
                    {
                        ReservationId = reader.GetInt32(0),
                        EventId = reader.GetInt32(1),
                        EventTitle = reader.GetString(2),
                        Location = reader.IsDBNull(3) ? null : reader.GetString(3),
                        EventDate = DateTime.Parse(reader.GetString(4)),
                        Quantity = reader.GetInt32(5)
                    });
                }
            }

            return Page();
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
