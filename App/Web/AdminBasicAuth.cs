using System.Security.Cryptography;
using System.Text;

namespace App.Web;

/// <summary>
/// HTTP Basic Auth for /admin. The page is disabled entirely (404) when
/// Admin__Password is not configured. Username is always "admin".
/// </summary>
public static class AdminBasicAuth
{
    public static IApplicationBuilder UseAdminBasicAuth(this IApplicationBuilder app, string? password)
    {
        return app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/admin"))
            {
                await next();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (IsAuthorized(context.Request.Headers.Authorization.ToString(), password))
            {
                await next();
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Hitta Evenemang admin\", charset=\"UTF-8\"";
        });
    }

    private static bool IsAuthorized(string header, string password)
    {
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
        }
        catch (FormatException)
        {
            return false;
        }

        var separator = decoded.IndexOf(':');
        if (separator < 0) return false;

        var user = decoded[..separator];
        var pass = decoded[(separator + 1)..];

        // Fixed-time comparison to avoid leaking password length/prefix via timing.
        var userOk = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(user), Encoding.UTF8.GetBytes("admin"));
        var passOk = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(pass), Encoding.UTF8.GetBytes(password));

        return userOk && passOk;
    }
}
