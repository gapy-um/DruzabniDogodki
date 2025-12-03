using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;

namespace DruzabniDogodki.Pages.Events
{
    public class IndexModel : PageModel
    {
        public List<EventItem> Events { get; set; } = new();

        public class EventItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime EventDate { get; set; }
            public string? Location { get; set; }
        }

        private const string ConnectionString =
            "Data Source=/Users/timurisek/Documents/GitHub/DruzabniDogodki/DruzabniDogodki/druzabnidogodki.db";

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using (var createCmd = connection.CreateCommand())
            {
                createCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Events (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        EventDate TEXT NOT NULL,
                        Location TEXT,
                        UserId INTEGER NOT NULL,
                        FOREIGN KEY (UserId) REFERENCES Users(Id)
                    );
                ";
                createCmd.ExecuteNonQuery();
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT e.Id, e.Title, e.Description, e.EventDate, e.Location
                FROM Events e
                JOIN Users u ON e.UserId = u.Id
                WHERE u.UserName = $username
                ORDER BY e.EventDate;
            ";

            cmd.Parameters.AddWithValue("$username", username);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Events.Add(new EventItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    EventDate = DateTime.Parse(reader.GetString(3)),
                    Location = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            return Page();
        }

        // POST: Urejanje dogodka (iz modala)
        public IActionResult OnPostEdit(int id, string title, string? description, DateTime eventDate, string? location)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Events
                SET Title = $title,
                    Description = $desc,
                    EventDate = $date,
                    Location = $loc
                WHERE Id = $id
                  AND UserId = (SELECT Id FROM Users WHERE UserName = $username);
            ";

            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$desc", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$date", eventDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("$loc", (object?)location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$username", username);

            cmd.ExecuteNonQuery();

            return RedirectToPage();
        }

        // POST: Brisanje dogodka
        public IActionResult OnPostDelete(int id)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM Events
                WHERE Id = $id
                  AND UserId = (SELECT Id FROM Users WHERE UserName = $username);
            ";

            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$username", username);

            cmd.ExecuteNonQuery();

            return RedirectToPage();
        }
    }
}
