using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;

namespace DruzabniDogodki.Pages.Events
{
    public class CalendarModel : PageModel
    {
        public Dictionary<DateOnly, List<EventItem>> EventsByDay { get; set; } = new();
        public bool IsAdmin { get; set; }

        public class EventItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime EventDate { get; set; }
            public string? Location { get; set; }
        }

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            var isAdminStr = HttpContext.Session.GetString("IsAdmin");
            IsAdmin = bool.TryParse(isAdminStr, out var isAdmin) && isAdmin;

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Title, Description, EventDate, Location
                FROM Events
                ORDER BY EventDate;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var item = new EventItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    EventDate = DateTime.Parse(reader.GetString(3)),
                    Location = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
                var key = DateOnly.FromDateTime(item.EventDate);
                if (!EventsByDay.TryGetValue(key, out var list))
                {
                    list = new List<EventItem>();
                    EventsByDay[key] = list;
                }
                list.Add(item);
            }

            return Page();
        }
    }
}
