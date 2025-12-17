using Microsoft.AspNetCore.Http;

namespace DruzabniDogodki.Helpers
{
    public static class AuthGuards
    {
        public static bool IsLoggedIn(HttpContext ctx)
            => !string.IsNullOrEmpty(ctx.Session.GetString("UserName"));

        public static bool IsAdmin(HttpContext ctx)
            => ctx.Session.GetString("Role") == "Admin";
    }
}
