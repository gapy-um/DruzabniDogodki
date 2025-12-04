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
        public bool IsAdmin { get; set; }

        public class EventItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime EventDate { get; set; }
            public string? Location { get; set; }
        }

        private const string ConnectionString =
            "Data Source=druzabnidogodki.db";

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            var isAdminStr = HttpContext.Session.GetString("IsAdmin");
            IsAdmin = bool.TryParse(isAdminStr, out var isAdmin) && isAdmin;

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
                ORDER BY e.EventDate;
            ";

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
    }
}
