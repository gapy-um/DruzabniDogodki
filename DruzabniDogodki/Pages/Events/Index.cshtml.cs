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

        [BindProperty(SupportsGet = true)]
        public string? SearchTitle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchLocation { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SearchDate { get; set; }

        public class EventItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime EventDate { get; set; }
            public string? Location { get; set; }
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            const string connectionString = "Data Source=druzabnidogodki.db";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // tabela Events – èe ne obstaja, jo ustvari
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

            // osnovni SELECT
            var sql = @"
                SELECT e.Id, e.Title, e.Description, e.EventDate, e.Location
                FROM Events e
                JOIN Users u ON e.UserId = u.Id
                WHERE u.UserName = $username
            ";

            using var cmd = connection.CreateCommand();
            cmd.Parameters.AddWithValue("$username", username);

            // filtri
            if (!string.IsNullOrWhiteSpace(SearchTitle))
            {
                sql += " AND e.Title LIKE $title";
                cmd.Parameters.AddWithValue("$title", "%" + SearchTitle + "%");
            }

            if (!string.IsNullOrWhiteSpace(SearchLocation))
            {
                sql += " AND e.Location LIKE $loc";
                cmd.Parameters.AddWithValue("$loc", "%" + SearchLocation + "%");
            }

            if (SearchDate.HasValue)
            {
                sql += " AND date(e.EventDate) = $date";
                cmd.Parameters.AddWithValue("$date", SearchDate.Value.ToString("yyyy-MM-dd"));
            }

            sql += " ORDER BY e.EventDate;";
            cmd.CommandText = sql;

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


        public IActionResult OnPostEdit(int id, string title, string? description, DateTime eventDate, string? location)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            const string connectionString = "Data Source=druzabnidogodki.db";

            using var connection = new SqliteConnection(connectionString);
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


        public IActionResult OnPostDelete(int id)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            const string connectionString = "Data Source=druzabnidogodki.db";

            using var connection = new SqliteConnection(connectionString);
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
