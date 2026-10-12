using System.Text;
using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.ApiTesting;

// What came back from one HTTP call, reduced to the parts the lesson prints and checks.
internal record ApiReply(int Status, string Body, string? ContentType, string? Location);

// Small helpers shared by the lesson methods: send a request and read the reply, find the shipped
// files. Helper code only: the lesson is in ApiTestingDemo.cs.
internal static class ApiClientHelpers
{
    // Sends one request and reads the whole reply. HttpClient also has async methods; they are taught
    // later (async/await comes later), so the lesson blocks on Send and ReadAsStream instead.
    public static ApiReply Send(HttpClient client, HttpMethod method, string path, string? jsonBody = null, string mediaType = "application/json")
    {
        using var request = new HttpRequestMessage(method, path);
        if (jsonBody != null) request.Content = new StringContent(jsonBody, Encoding.UTF8, mediaType);
        return Read(client.Send(request));
    }

    // Turns an HttpResponseMessage into an ApiReply and disposes the response.
    public static ApiReply Read(HttpResponseMessage response)
    {
        using (response)
        {
            using var reader = new StreamReader(response.Content.ReadAsStream());
            string body = reader.ReadToEnd();
            return new ApiReply((int)response.StatusCode, body,
                response.Content.Headers.ContentType?.ToString(), response.Headers.Location == null ? null : response.Headers.Location.IsAbsoluteUri ? response.Headers.Location.AbsolutePath : response.Headers.Location.OriginalString);
        }
    }

    // Finds a file shipped next to the lesson code (ApiTesting/<name>). The program runs from bin/Debug/...
    // (the csproj does not copy these files there), so look upward from the program's folder and from the
    // folder the user ran it in. Returns null when not found; the lesson then uses its embedded copy.
    public static string? FindShippedFile(string name)
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            DirectoryInfo? folder = new DirectoryInfo(start);
            for (int level = 0; folder != null && level < MaxLevelsUp; level++, folder = folder.Parent)
            {
                string candidate = Path.Combine(folder.FullName, "ApiTesting", name);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(folder.FullName, "BasicToAdvancedLearning.Console", "ApiTesting", name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    private const int MaxLevelsUp = 8;
}
