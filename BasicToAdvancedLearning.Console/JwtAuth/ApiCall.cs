using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BasicToAdvancedLearning.JwtAuth;

// What came back from one HTTP call: the status code, the body text and the WWW-Authenticate header text
// (the header a server sends with a 401 to say how to log in; empty when there was none).
internal sealed record ApiReply(HttpStatusCode StatusCode, string Body, string Challenge)
{
    // "401 Unauthorized", "403 Forbidden", "200 OK" - the number plus the standard name.
    public string Summary => $"{(int)StatusCode} {StatusCode}";
}

// Small helpers so the lesson methods stay about security, not about HttpClient plumbing.
internal static class ApiCall
{
    // Sends a GET. When token is given it goes in the header:  Authorization: <scheme> <token>
    public static ApiReply Get(HttpClient client, string path, string? token = null, string scheme = "Bearer")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, token);
        }
        return Send(client, request);
    }

    // Sends POST /login with a JSON body.
    public static ApiReply PostLogin(HttpClient client, string userName, string password)
    {
        string json = JsonSerializer.Serialize(new LoginRequest(userName, password));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/login")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        return Send(client, request);
    }

    // Logs in and returns just the token text (the lesson never prints it: tokens differ on every run).
    public static string Login(HttpClient client, string userName)
    {
        ApiReply reply = PostLogin(client, userName, DemoUsers.DemoPassword);
        using JsonDocument body = JsonDocument.Parse(reply.Body);
        return body.RootElement.GetProperty("token").GetString()!;
    }

    private static ApiReply Send(HttpClient client, HttpRequestMessage request)
    {
        // async/await comes later in the course, so we block until the reply arrives.
        using HttpResponseMessage response = client.Send(request);
        string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        string challenge = string.Join(" | ", response.Headers.WwwAuthenticate.Select(h => h.ToString()));
        return new ApiReply(response.StatusCode, body, challenge);
    }
}

// Reading a token by hand: a JWT is three base64url pieces joined by dots. base64url is just a way to
// write bytes as URL-safe text - anyone can decode it, no key needed. It is NOT encryption.
internal static class TokenParts
{
    public static byte[] DecodeBytes(string part)
    {
        string base64 = part.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }

    public static string DecodeText(string part) => Encoding.UTF8.GetString(DecodeBytes(part));

    public static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string EncodeText(string text) => Encode(Encoding.UTF8.GetBytes(text));

    // The field names of a JSON object, sorted so the output is the same on every run.
    public static string FieldNames(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return string.Join(", ", document.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    public static string GetText(string json, string field)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(field).GetString()!;
    }

    public static long GetNumber(string json, string field)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(field).GetInt64();
    }
}
