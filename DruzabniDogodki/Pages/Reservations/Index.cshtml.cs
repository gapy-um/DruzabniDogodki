using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DruzabniDogodki.Pages.Reservations
{
    public class IndexModel : PageModel
    {
        public List<ReservationItem> Reservations { get; set; } = new();
        public string Error { get; set; } = "";
        public string Message { get; set; } = "";

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public class ReservationItem
        {
            public int Id { get; set; }
            public int EventId { get; set; }
            public string EventTitle { get; set; } = "";
            public DateTime EventDate { get; set; }
            public string? Location { get; set; }
            public int Quantity { get; set; }
            public string CreatedAt { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // userId
            int userId;
            using (var userCmd = connection.CreateCommand())
            {
                userCmd.CommandText = "SELECT Id FROM Users WHERE UserName = $u LIMIT 1;";
                userCmd.Parameters.AddWithValue("$u", username);
                var r = userCmd.ExecuteScalar();
                if (r == null) return RedirectToPage("/Login");
                userId = Convert.ToInt32(r);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT r.Id, r.EventId, r.Quantity, r.CreatedAt,
       e.Title, e.EventDate, e.Location
FROM Reservations r
JOIN Events e ON e.Id = r.EventId
WHERE r.UserId = $uid
ORDER BY r.CreatedAt DESC;
";
            cmd.Parameters.AddWithValue("$uid", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Reservations.Add(new ReservationItem
                {
                    Id = reader.GetInt32(0),
                    EventId = reader.GetInt32(1),
                    Quantity = reader.GetInt32(2),
                    CreatedAt = reader.GetString(3),
                    EventTitle = reader.GetString(4),
                    EventDate = DateTime.Parse(reader.GetString(5)),
                    Location = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return Page();
        }

        public IActionResult OnPostCancel(int id)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // userId
            int userId;
            using (var userCmd = connection.CreateCommand())
            {
                userCmd.CommandText = "SELECT Id FROM Users WHERE UserName = $u LIMIT 1;";
                userCmd.Parameters.AddWithValue("$u", username);
                var r = userCmd.ExecuteScalar();
                if (r == null) return RedirectToPage("/Login");
                userId = Convert.ToInt32(r);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
DELETE FROM Reservations
WHERE Id = $id AND UserId = $uid;
";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$uid", userId);

            cmd.ExecuteNonQuery();

            return RedirectToPage();
        }
    }
}
