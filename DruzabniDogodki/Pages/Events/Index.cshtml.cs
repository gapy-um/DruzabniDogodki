using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using DruzabniDogodki.Helpers;

namespace DruzabniDogodki.Pages.Events
{
    public class IndexModel : PageModel
    {
        public List<EventItem> Events { get; set; } = new();
        public List<string> Locations { get; } = SloveniaLocations.All;

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? LocationFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Sort { get; set; } = "date_asc";

        public class EventItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime EventDate { get; set; }
            public string? Location { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? OwnerUserName { get; set; }
        }

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        private bool IsAdmin()
        {
            var u = HttpContext.Session.GetString("UserName") ?? "";
            var r = HttpContext.Session.GetString("Role") ?? "";
            return u.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   r.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            if (!IsAdmin())
                return RedirectToPage("/Events/All");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var sql = new StringBuilder(@"
SELECT e.Id, e.Title, e.Description, e.EventDate, e.Location, e.Latitude, e.Longitude, u.UserName
FROM Events e
JOIN Users u ON e.UserId = u.Id
WHERE 1=1
");

            using var cmd = connection.CreateCommand();

            if (!string.IsNullOrWhiteSpace(Q))
            {
                sql.Append(" AND (e.Title LIKE $q OR e.Description LIKE $q) ");
                cmd.Parameters.AddWithValue("$q", "%" + Q.Trim() + "%");
            }

            if (!string.IsNullOrWhiteSpace(LocationFilter))
            {
                sql.Append(" AND e.Location = $loc ");
                cmd.Parameters.AddWithValue("$loc", LocationFilter);
            }

            if (From.HasValue)
            {
                sql.Append(" AND e.EventDate >= $from ");
                cmd.Parameters.AddWithValue("$from", From.Value.ToString("yyyy-MM-dd HH:mm"));
            }

            if (To.HasValue)
            {
                sql.Append(" AND e.EventDate <= $to ");
                cmd.Parameters.AddWithValue("$to", To.Value.ToString("yyyy-MM-dd HH:mm"));
            }

            sql.Append(Sort switch
            {
                "date_desc" => " ORDER BY e.EventDate DESC ",
                "title_asc" => " ORDER BY e.Title ASC ",
                _ => " ORDER BY e.EventDate ASC "
            });

            cmd.CommandText = sql.ToString();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Events.Add(new EventItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    EventDate = DateTime.Parse(reader.GetString(3)),
                    Location = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Latitude = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                    Longitude = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                    OwnerUserName = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return Page();
        }

        public IActionResult OnPostEdit(
            int id,
            string title,
            string? description,
            DateTime eventDate,
            string location,
            double latitude,
            double longitude)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            if (!IsAdmin())
                return RedirectToPage("/Events/All");

            if (string.IsNullOrWhiteSpace(location) || !Locations.Contains(location))
                return RedirectToPage();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
UPDATE Events
SET Title = $title,
    Description = $desc,
    EventDate = $date,
    Location = $loc,
    Latitude = $lat,
    Longitude = $lng
WHERE Id = $id;
";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$desc", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$date", eventDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("$loc", location);
            cmd.Parameters.AddWithValue("$lat", latitude);
            cmd.Parameters.AddWithValue("$lng", longitude);

            cmd.ExecuteNonQuery();
            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            if (!IsAdmin())
                return RedirectToPage("/Events/All");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Events WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();

            return RedirectToPage();
        }
    }
}
