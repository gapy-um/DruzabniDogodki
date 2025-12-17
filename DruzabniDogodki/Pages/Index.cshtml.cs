using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace DruzabniDogodki.Pages
{
    public class IndexModel : PageModel
    {
        public List<string> EventDates { get; set; } = new(); // "yyyy-MM-dd"

        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        public void OnGet()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT EventDate
FROM Events;
";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dtText = reader.GetString(0); // "yyyy-MM-dd HH:mm"
                if (DateTime.TryParse(dtText, out var dt))
                {
                    var day = dt.ToString("yyyy-MM-dd");
                    if (!EventDates.Contains(day))
                        EventDates.Add(day);
                }
            }
        }
    }
}
