using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;

namespace DruzabniDogodki.Pages.Events
{
    public class DetailsModel : PageModel
    {
        private const string ConnectionString = "Data Source=druzabnidogodki.db";

        // Event fields
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // UI state
        public bool IsLoggedIn { get; set; }
        public bool IsAdmin { get; set; }
        public int? CurrentUserId { get; set; }
        public string? CurrentUserName { get; set; }

        public string? Message { get; set; }
        public string? Error { get; set; }

        // Ratings
        public double AvgStars { get; set; }
        public int RatingsCount { get; set; }
        public int? UserStars { get; set; }

        // Comments
        public List<CommentItem> Comments { get; set; } = new();

        public class CommentItem
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; } = "";
            public string Content { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public bool CanDelete { get; set; }
        }

        private bool ComputeIsAdmin()
        {
            var u = HttpContext.Session.GetString("UserName") ?? "";
            var r = HttpContext.Session.GetString("Role") ?? "";
            return u.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   r.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }

        private int? GetCurrentUserId(SqliteConnection connection)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username)) return null;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Id FROM Users WHERE UserName = $u LIMIT 1;";
            cmd.Parameters.AddWithValue("$u", username);

            var res = cmd.ExecuteScalar();
            if (res == null) return null;
            return Convert.ToInt32(res);
        }

        private void EnsureCommentAndRatingTables(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Comments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (EventId) REFERENCES Events(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS Ratings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EventId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    Stars INTEGER NOT NULL CHECK (Stars >= 1 AND Stars <= 5),
    CreatedAt TEXT NOT NULL,
    UNIQUE(EventId, UserId),
    FOREIGN KEY (EventId) REFERENCES Events(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
";
                cmd.ExecuteNonQuery();
            }
        }

        public IActionResult OnGet(int id, string? message = null, string? error = null)
        {
            Message = message;
            Error = error;

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            EnsureCommentAndRatingTables(connection);

            // user context
            CurrentUserName = HttpContext.Session.GetString("UserName");
            IsLoggedIn = !string.IsNullOrEmpty(CurrentUserName);
            IsAdmin = ComputeIsAdmin();
            CurrentUserId = IsLoggedIn ? GetCurrentUserId(connection) : null;

            // load event
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
SELECT Id, Title, Description, EventDate, Location, Latitude, Longitude
FROM Events
WHERE Id = $id
LIMIT 1;
";
                cmd.Parameters.AddWithValue("$id", id);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return RedirectToPage("/Events/All");

                Id = reader.GetInt32(0);
                Title = reader.GetString(1);
                Description = reader.IsDBNull(2) ? null : reader.GetString(2);
                EventDate = DateTime.Parse(reader.GetString(3));
                Location = reader.IsDBNull(4) ? null : reader.GetString(4);
                Latitude = reader.IsDBNull(5) ? null : reader.GetDouble(5);
                Longitude = reader.IsDBNull(6) ? null : reader.GetDouble(6);
            }

            LoadRatings(connection);
            LoadComments(connection);

            return Page();
        }

        private void LoadRatings(SqliteConnection connection)
        {
            AvgStars = 0;
            RatingsCount = 0;
            UserStars = null;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
SELECT 
    COALESCE(AVG(Stars), 0),
    COUNT(*)
FROM Ratings
WHERE EventId = $eid;
";
                cmd.Parameters.AddWithValue("$eid", Id);

                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    AvgStars = r.GetDouble(0);
                    RatingsCount = r.GetInt32(1);
                }
            }

            if (CurrentUserId.HasValue)
            {
                using var cmd2 = connection.CreateCommand();
                cmd2.CommandText = @"
SELECT Stars
FROM Ratings
WHERE EventId = $eid AND UserId = $uid
LIMIT 1;
";
                cmd2.Parameters.AddWithValue("$eid", Id);
                cmd2.Parameters.AddWithValue("$uid", CurrentUserId.Value);

                var res = cmd2.ExecuteScalar();
                if (res != null)
                    UserStars = Convert.ToInt32(res);
            }
        }

        private void LoadComments(SqliteConnection connection)
        {
            Comments.Clear();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT c.Id, c.UserId, u.UserName, c.Content, c.CreatedAt
FROM Comments c
JOIN Users u ON u.Id = c.UserId
WHERE c.EventId = $eid
ORDER BY c.CreatedAt DESC;
";
            cmd.Parameters.AddWithValue("$eid", Id);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var commentUserId = reader.GetInt32(1);

                Comments.Add(new CommentItem
                {
                    Id = reader.GetInt32(0),
                    UserId = commentUserId,
                    UserName = reader.GetString(2),
                    Content = reader.GetString(3),
                    CreatedAt = DateTime.Parse(reader.GetString(4)),
                    CanDelete = IsAdmin || (CurrentUserId.HasValue && CurrentUserId.Value == commentUserId)
                });
            }
        }

        // -----------------------------
        // RESERVATION (obdržiš kar imaš)
        // -----------------------------
        public IActionResult OnPostReserve(int id, int quantity)
{
    var username = HttpContext.Session.GetString("UserName");
    if (string.IsNullOrEmpty(username))
        return RedirectToPage("/Login");

    if (quantity < 1 || quantity > 10)
        return RedirectToPage(new { id, error = "Neveljavna količina (1–10)." });

    using var connection = new SqliteConnection(ConnectionString);
    connection.Open();

    // userId
    int userId;
    using (var ucmd = connection.CreateCommand())
    {
        ucmd.CommandText = @"SELECT Id FROM Users WHERE UserName = $u LIMIT 1;";
        ucmd.Parameters.AddWithValue("$u", username);
        var res = ucmd.ExecuteScalar();
        if (res == null)
            return RedirectToPage(new { id, error = "Uporabnik ne obstaja." });

        userId = Convert.ToInt32(res);
    }

    // insert reservation (z CreatedAt)
    using (var cmd = connection.CreateCommand())
    {
        cmd.CommandText = @"
INSERT INTO Reservations (UserId, EventId, Quantity, CreatedAt)
VALUES ($uid, $eid, $q, $t);
";
        cmd.Parameters.AddWithValue("$uid", userId);
        cmd.Parameters.AddWithValue("$eid", id);
        cmd.Parameters.AddWithValue("$q", quantity);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        cmd.ExecuteNonQuery();
    }

    return RedirectToPage(new { id, message = "Rezervacija uspešna!" });
}


        // -----------------------------
        // COMMENTS
        // -----------------------------
        public IActionResult OnPostAddComment(int id, string content)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            content = (content ?? "").Trim();
            if (content.Length < 2)
                return RedirectToPage(new { id, error = "Komentar je prekratek." });

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            EnsureCommentAndRatingTables(connection);

            // userId
            int userId;
            using (var ucmd = connection.CreateCommand())
            {
                ucmd.CommandText = @"SELECT Id FROM Users WHERE UserName = $u LIMIT 1;";
                ucmd.Parameters.AddWithValue("$u", username);
                var res = ucmd.ExecuteScalar();
                if (res == null)
                    return RedirectToPage(new { id, error = "Uporabnik ne obstaja." });

                userId = Convert.ToInt32(res);
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO Comments (EventId, UserId, Content, CreatedAt)
VALUES ($eid, $uid, $c, $t);
";
                cmd.Parameters.AddWithValue("$eid", id);
                cmd.Parameters.AddWithValue("$uid", userId);
                cmd.Parameters.AddWithValue("$c", content);
                cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
            }

            return RedirectToPage(new { id, message = "Komentar dodan." });
        }

        public IActionResult OnPostDeleteComment(int eventId, int commentId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToPage("/Login");

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            EnsureCommentAndRatingTables(connection);

            // kdo je prijavljen
            var isAdmin = ComputeIsAdmin();
            var currentUserId = GetCurrentUserId(connection);

            if (!currentUserId.HasValue)
                return RedirectToPage(new { id = eventId, error = "Napaka pri uporabniku." });

            // preveri ownerja komentarja
            int commentUserId;
            using (var check = connection.CreateCommand())
            {
                check.CommandText = @"SELECT UserId FROM Comments WHERE Id = $cid AND EventId = $eid LIMIT 1;";
                check.Parameters.AddWithValue("$cid", commentId);
                check.Parameters.AddWithValue("$eid", eventId);

                var res = check.ExecuteScalar();
                if (res == null)
                    return RedirectToPage(new { id = eventId });

                commentUserId = Convert.ToInt32(res);
            }

            if (!isAdmin && commentUserId != currentUserId.Value)
                return RedirectToPage(new { id = eventId, error = "Nimaš pravic za brisanje komentarja." });

            using (var del = connection.CreateCommand())
            {
                del.CommandText = @"DELETE FROM Comments WHERE Id = $cid;";
                del.Parameters.AddWithValue("$cid", commentId);
                del.ExecuteNonQuery();
            }

            return RedirectToPage(new { id = eventId, message = "Komentar izbrisan." });
        }

        // -----------------------------
        // RATINGS (1x na user na event)
        // -----------------------------
        public IActionResult OnPostRate(int id, int stars)
        {
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            if (stars < 1 || stars > 5)
                return RedirectToPage(new { id, error = "Ocena mora biti 1–5." });

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            EnsureCommentAndRatingTables(connection);

            // userId
            int userId;
            using (var ucmd = connection.CreateCommand())
            {
                ucmd.CommandText = @"SELECT Id FROM Users WHERE UserName = $u LIMIT 1;";
                ucmd.Parameters.AddWithValue("$u", username);
                var res = ucmd.ExecuteScalar();
                if (res == null)
                    return RedirectToPage(new { id, error = "Uporabnik ne obstaja." });

                userId = Convert.ToInt32(res);
            }

            // Insert or update
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO Ratings (EventId, UserId, Stars, CreatedAt)
VALUES ($eid, $uid, $s, $t)
ON CONFLICT(EventId, UserId)
DO UPDATE SET Stars = $s, CreatedAt = $t;
";
                cmd.Parameters.AddWithValue("$eid", id);
                cmd.Parameters.AddWithValue("$uid", userId);
                cmd.Parameters.AddWithValue("$s", stars);
                cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();
            }

            return RedirectToPage(new { id, message = "Ocena shranjena." });
        }
    }
}
