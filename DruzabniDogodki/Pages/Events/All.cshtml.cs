using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using DruzabniDogodki.Helpers;

namespace DruzabniDogodki.Pages.Events
{
    public class AllModel : PageModel
    {
        public List<EventItem> Events { get; set; } = new();
        public List<string> Locations { get; } = SloveniaLocations.All;

        // FILTERS (GET)
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

            // ⭐ ratings
            public double AvgRating { get; set; }
            public int RatingCount { get; set; }
        }

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public void OnGet()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var sql = new StringBuilder(@"
SELECT
    e.Id,
    e.Title,
    e.Description,
    e.EventDate,
    e.Location,
    IFNULL(AVG(r.Stars), 0) AS AvgRating,
    COUNT(r.Id) AS RatingCount
FROM Events e
LEFT JOIN Ratings r ON r.EventId = e.Id
WHERE 1=1
");

            using var cmd = connection.CreateCommand();

            // search
            if (!string.IsNullOrWhiteSpace(Q))
            {
                sql.Append(" AND (e.Title LIKE $q OR e.Description LIKE $q) ");
                cmd.Parameters.AddWithValue("$q", "%" + Q.Trim() + "%");
            }

            // location filter
            if (!string.IsNullOrWhiteSpace(LocationFilter))
            {
                sql.Append(" AND e.Location = $loc ");
                cmd.Parameters.AddWithValue("$loc", LocationFilter);
            }

            // date range
            if (From.HasValue)
            {
                sql.Append(" AND e.EventDate >= $from ");
                cmd.Parameters.AddWithValue("$from", From.Value.ToString("yyyy-MM-dd 00:00"));
            }

            if (To.HasValue)
            {
                sql.Append(" AND e.EventDate <= $to ");
                cmd.Parameters.AddWithValue("$to", To.Value.ToString("yyyy-MM-dd 23:59"));
            }

            // group by (zaradi AVG/COUNT)
            sql.Append(@"
GROUP BY e.Id, e.Title, e.Description, e.EventDate, e.Location
");

            // sort
            sql.Append(Sort switch
            {
                "date_desc" => " ORDER BY e.EventDate DESC ",
                "title_asc" => " ORDER BY e.Title ASC ",
                "title_desc" => " ORDER BY e.Title DESC ",
                "rating_desc" => " ORDER BY AvgRating DESC, RatingCount DESC ",
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
                    AvgRating = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                    RatingCount = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
                });
            }
        }
    }
}
