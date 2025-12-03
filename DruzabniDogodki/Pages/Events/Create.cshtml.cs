using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;

namespace DruzabniDogodki.Pages.Events
{
    public class CreateModel : PageModel
    {
        [BindProperty]
        public EventInput Event { get; set; } = new();

        public class EventInput
        {
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime EventDate { get; set; } = DateTime.Now;
            public string? Location { get; set; }
        }

        public IActionResult OnGet()
        {
            // preverimo, če je uporabnik prijavljen (po UserName)
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // uporabi ISTI connection string kot pri loginu!
            const string connectionString = "Data Source=druzabnidogodki.db";

            Console.WriteLine("DB PATH = " + connectionString);



            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Events (Title, Description, EventDate, Location, UserId)
                VALUES (
                    $title, 
                    $desc, 
                    $date, 
                    $loc,
                    (SELECT Id FROM Users WHERE UserName = $username)
                );";

            cmd.Parameters.AddWithValue("$title", Event.Title);
            cmd.Parameters.AddWithValue("$desc", (object?)Event.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$date", Event.EventDate.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("$loc", (object?)Event.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$username", username);

            cmd.ExecuteNonQuery();

           return RedirectToPage("/Events/Index");

        }
    }
}
