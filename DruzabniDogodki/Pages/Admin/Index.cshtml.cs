using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DruzabniDogodki.Pages.Admin
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
            public string? Organizer { get; set; }
            public string? Category { get; set; }
            public decimal? Price { get; set; }
        }

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            if (!string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            // Ustvarimo tabelo Users, èe še ne obstaja
            using (var createUsersCmd = connection.CreateCommand())
            {
                createUsersCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserName TEXT NOT NULL UNIQUE,
                        Email TEXT NOT NULL,
                        Password TEXT NOT NULL,
                        IsAdmin INTEGER DEFAULT 0
                    );
                ";
                createUsersCmd.ExecuteNonQuery();
            }

            LoadEvents();
            return Page();
        }

        private void LoadEvents()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Title, Description, EventDate, Location, Organizer, Category, Price
                FROM Events
                ORDER BY EventDate;
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
                    Location = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Organizer = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Category = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Price = reader.IsDBNull(7) ? null : reader.GetDecimal(7)
                });
            }
        }

        public async Task<IActionResult> OnPostCreateAsync(string title, string? description, DateTime eventDate,
            string? location, string? organizer, string? category, decimal? price, IFormFile? image)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username) || !string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            string? imagePath = null;
            if (image != null && image.Length > 0)
            {
                // Validate file size (max 5 MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("image", "Slika ne sme biti veèja od 5 MB.");
                    LoadEvents();
                    return Page();
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("image", "Dovoljene so samo slike (.jpg, .png, .gif).");
                    LoadEvents();
                    return Page();
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "events");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    imagePath = $"/images/events/{uniqueFileName}";
                }
                catch
                {
                    ModelState.AddModelError("image", "Napaka pri nalaganju slike.");
                    LoadEvents();
                    return Page();
                }
            }

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Events (Title, Description, EventDate, Location, Organizer, Category, Price, ImagePath, UserId)
                VALUES ($title, $desc, $date, $loc, $org, $cat, $price, $img, (SELECT Id FROM Users WHERE UserName = $username));
            ";

            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$desc", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$date", eventDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("$loc", (object?)location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$org", (object?)organizer ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$cat", (object?)category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$price", (object?)price ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$img", (object?)imagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$username", username);

            cmd.ExecuteNonQuery();

            return RedirectToPage();
        }

        public IActionResult OnPostEdit(int id, string title, string? description, DateTime eventDate,
            string? location, string? organizer, string? category, decimal? price)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username) || !string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Events
                SET Title = $title, Description = $desc, EventDate = $date, Location = $loc,
                    Organizer = $org, Category = $cat, Price = $price
                WHERE Id = $id;
            ";

            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$desc", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$date", eventDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("$loc", (object?)location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$org", (object?)organizer ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$cat", (object?)category ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$price", (object?)price ?? DBNull.Value);

            cmd.ExecuteNonQuery();

            return RedirectToPage();
        }

        public IActionResult OnPostDelete(int id)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username) || !string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();

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