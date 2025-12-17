using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DruzabniDogodki.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public List<TopEventRow> TopEvents { get; set; } = new();
        public List<TopUserRow> TopUsers { get; set; } = new();

        // Graf: Tickets po dnevu (YYYY-MM-DD -> sum)
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartValues { get; set; } = new();

        public int TotalTickets { get; set; }
        public int TotalReservations { get; set; }

        public class TopEventRow
        {
            public int EventId { get; set; }
            public string Title { get; set; } = "";
            public string? Location { get; set; }
            public DateTime EventDate { get; set; }
            public int Tickets { get; set; }
        }

        public class TopUserRow
        {
            public int UserId { get; set; }
            public string UserName { get; set; } = "";
            public int Tickets { get; set; }
            public int ReservationsCount { get; set; }
        }

        public IActionResult OnGet()
        {
            if (!IsAdmin())
                return RedirectToPage("/Index");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            LoadTotals(connection);
            LoadTopEvents(connection);
            LoadTopUsers(connection);
            LoadChart(connection);

            return Page();
        }

        private void LoadTotals(SqliteConnection connection)
        {
            // total tickets + reservations count
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 
    COALESCE(SUM(Quantity), 0) AS TotalTickets,
    COALESCE(COUNT(*), 0)      AS TotalReservations
FROM Reservations;
";
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    TotalTickets = r.GetInt32(0);
                    TotalReservations = r.GetInt32(1);
                }
            }
        }

        private void LoadTopEvents(SqliteConnection connection)
        {
            TopEvents.Clear();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT 
    e.Id,
    e.Title,
    e.Location,
    e.EventDate,
    COALESCE(SUM(r.Quantity), 0) AS Tickets
FROM Events e
JOIN Reservations r ON r.EventId = e.Id
GROUP BY e.Id, e.Title, e.Location, e.EventDate
ORDER BY Tickets DESC
LIMIT 5;
";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                TopEvents.Add(new TopEventRow
                {
                    EventId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Location = reader.IsDBNull(2) ? null : reader.GetString(2),
                    EventDate = DateTime.Parse(reader.GetString(3)),
                    Tickets = reader.GetInt32(4)
                });
            }
        }

        private void LoadTopUsers(SqliteConnection connection)
        {
            TopUsers.Clear();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT 
    u.Id,
    u.UserName,
    COALESCE(SUM(r.Quantity), 0) AS Tickets,
    COUNT(r.Id) AS ReservationsCount
FROM Users u
JOIN Reservations r ON r.UserId = u.Id
GROUP BY u.Id, u.UserName
ORDER BY Tickets DESC
LIMIT 5;
";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                TopUsers.Add(new TopUserRow
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Tickets = reader.GetInt32(2),
                    ReservationsCount = reader.GetInt32(3)
                });
            }
        }

        private void LoadChart(SqliteConnection connection)
        {
            // Graf: zadnjih 14 dni po EventDate dnevu: sum ticketov
            // (Če želiš po datumu rezervacije, rabiš stolpec CreatedAt v Reservations)
            var map = new Dictionary<string, int>(); // yyyy-MM-dd -> tickets

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 
    substr(e.EventDate, 1, 10) AS DayKey,
    COALESCE(SUM(r.Quantity), 0) AS Tickets
FROM Reservations r
JOIN Events e ON e.Id = r.EventId
GROUP BY DayKey
ORDER BY DayKey DESC
LIMIT 14;
";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var day = reader.GetString(0);
                    var tickets = reader.GetInt32(1);
                    map[day] = tickets;
                }
            }

            // uredimo naraščajoče po dnevu
            var ordered = map.Keys.OrderBy(x => x).ToList();

            ChartLabels = ordered;
            ChartValues = ordered.Select(k => map[k]).ToList();
        }

        private bool IsAdmin()
        {
            var u = HttpContext.Session.GetString("UserName") ?? "";
            var r = HttpContext.Session.GetString("Role") ?? "";
            return u.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   r.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
