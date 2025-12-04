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
        public DateOnly CurrentMonth { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1 - DateTime.Today.Day));
        public DateOnly PrevMonth => CurrentMonth.AddMonths(-1);
        public DateOnly NextMonth => CurrentMonth.AddMonths(1);

        public class EventItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public DateTime EventDate { get; set; }
            public string? Location { get; set; }
        }

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public IActionResult OnGet(string? month)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            var isAdminStr = HttpContext.Session.GetString("IsAdmin");
            IsAdmin = bool.TryParse(isAdminStr, out var isAdmin) && isAdmin;

            // Parse month yyyy-MM, default to current month
            if (!string.IsNullOrWhiteSpace(month))
            {
                if (DateTime.TryParse(month + "-01", out var parsed))
                {
                    CurrentMonth = new DateOnly(parsed.Year, parsed.Month, 1);
                }
            }

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Title, Description, EventDate, Location
                FROM Events
                WHERE strftime('%Y-%m', EventDate) = $month
                ORDER BY EventDate;";
            cmd.Parameters.AddWithValue("$month", $"{CurrentMonth.Year:D4}-{CurrentMonth.Month:D2}");

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
