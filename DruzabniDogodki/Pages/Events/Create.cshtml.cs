using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using DruzabniDogodki.Helpers;

namespace DruzabniDogodki.Pages.Events
{
    public class CreateModel : PageModel
    {
        [BindProperty]
        public EventInput Event { get; set; } = new();

        [BindProperty]
        public double? Latitude { get; set; }

        [BindProperty]
        public double? Longitude { get; set; }

        public List<string> Locations { get; } = SloveniaLocations.All;

        public class EventInput
        {
            [Required(ErrorMessage = "Naslov je obvezen.")]
            public string Title { get; set; } = "";

            public string? Description { get; set; }

            [Required(ErrorMessage = "Datum je obvezen.")]
            public DateTime EventDate { get; set; } = DateTime.Now;

            [Required(ErrorMessage = "Izberi kraj.")]
            public string? Location { get; set; }
        }

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
                return RedirectToPage("/Index");

            return Page();
        }

        public IActionResult OnPost()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            if (!IsAdmin())
                return RedirectToPage("/Index");

            if (!ModelState.IsValid)
                return Page();

            if (string.IsNullOrWhiteSpace(Event.Location) || !Locations.Contains(Event.Location))
            {
                ModelState.AddModelError("Event.Location", "Izberi kraj iz seznama.");
                return Page();
            }

            if (Latitude == null || Longitude == null)
            {
                ModelState.AddModelError(string.Empty, "Izberi lokacijo na mapi (klikni na mapo).");
                return Page();
            }

            const string connectionString = "Data Source=druzabnidogodki.db";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // UserId
            int userId;
            using (var userCmd = connection.CreateCommand())
            {
                userCmd.CommandText = @"SELECT Id FROM Users WHERE UserName = $u LIMIT 1;";
                userCmd.Parameters.AddWithValue("$u", username);

                var result = userCmd.ExecuteScalar();
                if (result == null)
                {
                    HttpContext.Session.Remove("UserName");
                    HttpContext.Session.Remove("Role");
                    return RedirectToPage("/Login");
                }

                userId = Convert.ToInt32(result);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Events (Title, Description, EventDate, Location, UserId, Latitude, Longitude)
VALUES ($title, $desc, $date, $loc, $userId, $lat, $lng);
";
            cmd.Parameters.AddWithValue("$title", Event.Title);
            cmd.Parameters.AddWithValue("$desc", (object?)Event.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$date", Event.EventDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("$loc", Event.Location);
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$lat", Latitude.Value);
            cmd.Parameters.AddWithValue("$lng", Longitude.Value);

            cmd.ExecuteNonQuery();

            return RedirectToPage("/Events/Index");
        }
    }
}
