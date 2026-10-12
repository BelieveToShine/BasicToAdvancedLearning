using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.HttpAndRest;

// Topic 16: HTTP and REST fundamentals. HTTP is the plain-text conversation a client and a server have:
// the client sends a REQUEST, the server sends back one RESPONSE. REST is a habit for naming things in
// that conversation (URLs are nouns, the method is the verb). The running example is the bank's accounts.
//
// A real Web API is its own program that keeps running. To let this console lesson finish, BankApi starts
// a small server INSIDE this program (on a free local port, reachable only from this computer) and the
// lesson talks to it with HttpClient, exactly as it would talk to a server somewhere else. The port is
// never printed because it is different every run.
//
// Real code usually sends requests with async/await, which comes later in the course. Here the lesson
// uses the blocking versions, so each line finishes before the next one starts.
// See ArchitectureDiagrams/HttpAndRest for the visual walkthrough.
public class HttpAndRestDemo
{
    private const string JsonContentType = "application/json";

    // The anatomy of one exchange: what the client sends, what the server answers.
    public void ExplainRequestAndResponse()
    {
        PrintHeading("1. Request and response — the anatomy of one exchange");

        using RunningServer server = BankApi.Start();
        HttpClient client = server.Client;

        // A request is: a METHOD (what to do), a URL (which thing; its path and optional ?query), headers
        // (extra facts about the request) and, for some methods, a body. A GET normally has no body.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts?owner=Asha");
        request.Headers.Add("Accept", JsonContentType);   // "I would like JSON back" (this server sends JSON anyway)
        Uri fullUrl = new Uri(client.BaseAddress!, request.RequestUri!);
        Console.WriteLine($"Request  method : {request.Method}");
        Console.WriteLine($"Request  path   : {fullUrl.AbsolutePath}");
        Console.WriteLine($"Request  query  : {fullUrl.Query}");
        Console.WriteLine($"Request  header : Accept: {request.Headers.Accept}");

        // Send blocks until the server's response has arrived (async/await comes later).
        using HttpResponseMessage response = client.Send(request);

        // A response is: a status code (what happened), headers, and usually a body.
        Console.WriteLine($"Response status : {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"Response header : Content-Type: {response.Content.Headers.ContentType}");
        Console.WriteLine($"Response body   : {ReadBody(response)}");

        // HttpClient writes the text for you. Here the same kind of request is typed by hand over a plain
        // TCP connection, to show that HTTP really is lines of text: a request line (method, path,
        // version), header lines, then a blank line. The response is a status line, header lines, a blank
        // line, then the body. Two things in the reply are worth knowing: "Transfer-Encoding: chunked"
        // means the body arrives in pieces, each introduced by its size in hex (2c = 44 characters here)
        // and ended by a size of 0; and the Date header is left out of the print because it changes every run.
        string rawRequest = "GET /api/accounts/1 HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n";
        string rawResponse = SendRawText(fullUrl.Port, rawRequest);
        Console.WriteLine("Raw text the client sent:");
        PrintIndented(rawRequest);
        Console.WriteLine("Raw text the server sent back:");
        PrintIndented(rawResponse);
    }

    // The method says what you want done. Two properties matter: safe (does not change anything on the
    // server) and idempotent (doing it twice leaves the server in the same state as doing it once).
    public void ExplainHttpMethods()
    {
        PrintHeading("2. HTTP methods — GET, POST, PUT, DELETE; safe vs idempotent");

        using RunningServer server = BankApi.Start();
        HttpClient client = server.Client;
        string newAccount = "{\"owner\":\"Dana Ortiz\",\"balance\":100}";
        string replacement = "{\"owner\":\"Ben Carter\",\"balance\":900}";

        // GET reads. It is meant to be safe, so asking twice changes nothing.
        Console.WriteLine($"Start: {CountAccounts(client)} accounts");
        Send(client, HttpMethod.Get, "/api/accounts/1");
        Send(client, HttpMethod.Get, "/api/accounts/1");
        Console.WriteLine($"After GET twice: {CountAccounts(client)} accounts (safe: nothing changed)");

        // POST is not defined as idempotent; on this server each call creates a NEW account, so twice means
        // two new accounts, with two different ids chosen by the server.
        Send(client, HttpMethod.Post, "/api/accounts", newAccount);
        Send(client, HttpMethod.Post, "/api/accounts", newAccount);
        Console.WriteLine($"After POST twice: {CountAccounts(client)} accounts (not idempotent: two were created)");

        // PUT replaces ONE named thing with what you send. Doing it twice leaves the same result.
        Send(client, HttpMethod.Put, "/api/accounts/2", replacement);
        Send(client, HttpMethod.Put, "/api/accounts/2", replacement);
        Console.WriteLine($"After PUT twice: {CountAccounts(client)} accounts (idempotent: account 2 is simply replaced)");

        // DELETE removes. The SERVER ends in the same state after one call or two (the account is gone),
        // so it is idempotent, even though the second reply is different (404: nothing left to delete).
        Send(client, HttpMethod.Delete, "/api/accounts/3");
        Send(client, HttpMethod.Delete, "/api/accounts/3");
        Console.WriteLine($"After DELETE twice: {CountAccounts(client)} accounts (idempotent: account 3 is gone either way)");

        // Each URL accepts only some methods. Asking with a method the URL does not offer gets 405
        // (Method Not Allowed), and the Allow header lists the methods it does accept.
        using HttpResponseMessage wrongMethod = Fetch(client, HttpMethod.Delete, "/api/accounts");
        Console.WriteLine($"Allow header: {string.Join(", ", wrongMethod.Content.Headers.Allow)}");

        // Safe and idempotent are rules for how a server SHOULD behave. HTTP cannot force a server to keep
        // them: this server follows them because its author wrote it that way.
    }

    // The status code is the server's one-number answer to "how did it go?". The first digit is the family.
    public void ExplainStatusCodes()
    {
        PrintHeading("3. Status codes — 2xx ok, 3xx moved, 4xx client's fault, 5xx server's fault");

        using RunningServer server = BankApi.Start();
        HttpClient client = server.Client;

        // By default HttpClient follows a redirect for you. This second client turns that off so the
        // 3xx reply itself is visible.
        using var noRedirectClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(server.BaseAddress),
        };

        // 2xx: it worked. 200 OK = here is what you asked for. 201 Created = a new thing now exists.
        // 204 No Content = done, and there is nothing to send back.
        Send(client, HttpMethod.Get, "/api/accounts/1");
        Send(client, HttpMethod.Post, "/api/accounts", "{\"owner\":\"Dana Ortiz\",\"balance\":100}");
        Send(client, HttpMethod.Delete, "/api/accounts/3");

        // 3xx: look somewhere else. 301 = moved permanently; the Location header says where.
        using HttpResponseMessage moved = Fetch(noRedirectClient, HttpMethod.Get, "/api/old-accounts");
        Console.WriteLine($"  Location header on the 301: {moved.Headers.Location}");

        // 4xx: the REQUEST was the problem, so repeating it unchanged usually will not help.
        // 400 = the server could not accept what was sent. 404 = no such thing. 415 = a body in a format
        // the server does not read (here plain text sent to an endpoint that expects JSON).
        Send(client, HttpMethod.Post, "/api/accounts", "{\"owner\":\"\",\"balance\":-5}");
        using HttpResponseMessage notFound = Fetch(client, HttpMethod.Get, "/api/accounts/999");
        SendPlainText(client);

        // 5xx: the SERVER failed while handling a request that may have been fine. The body is not printed:
        // this lesson's server runs in Development mode, which puts a long error report (a stack trace with
        // file paths, and your request headers) in the body. A production server should not show that to its callers.
        Send(client, HttpMethod.Get, "/api/crash", showBody: false);

        // HttpClient does NOT throw for 404 or 500: getting an answer counts as success for it. Check the
        // code yourself, or call EnsureSuccessStatusCode to turn any non-2xx into an exception.
        try
        {
            notFound.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"EnsureSuccessStatusCode threw {nameof(HttpRequestException)}, StatusCode = {(int?)ex.StatusCode}");
        }
    }

    // JSON is the text format an API usually sends bodies in. System.Text.Json turns a C# object into
    // JSON text (serialize) and back into an object (deserialize).
    public void ExplainJsonBodies()
    {
        PrintHeading("4. JSON bodies — serialize, deserialize, Content-Type");

        var account = new Account(1, "Asha Rao", 500.00m);

        // JSON has objects { "name": value }, arrays [ ... ], strings in double quotes, numbers,
        // true/false and null. Property names are always in double quotes.
        string plain = JsonSerializer.Serialize(account);
        Console.WriteLine($"Default options : {plain}");

        // The default keeps the C# property names (Id, Owner). Web APIs usually write camelCase (id, owner),
        // which is what JsonSerializerDefaults.Web produces; ASP.NET Core uses those web defaults for its bodies.
        var webOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string camel = JsonSerializer.Serialize(account, webOptions);
        Console.WriteLine($"Web options     : {camel}");

        // Reading it back. With the default options, names must match the C# names exactly (including
        // upper/lower case). A camelCase text then fills in NOTHING and no error is raised: every
        // property keeps its default value. Web options accept the camelCase names.
        Account? wrongCase = JsonSerializer.Deserialize<Account>(camel);
        Console.WriteLine($"Default options reading camelCase: Id = {wrongCase!.Id}, Owner is null = {wrongCase.Owner == null}");
        Account? fromCamel = JsonSerializer.Deserialize<Account>(camel, webOptions);
        Console.WriteLine($"Web options reading camelCase    : {fromCamel}");

        // Sending: the body is just text, and the Content-Type header tells the server how to read it.
        using RunningServer server = BankApi.Start();
        string inputJson = JsonSerializer.Serialize(new AccountInput("Dana Ortiz", 100m), webOptions);
        using HttpResponseMessage response = Fetch(server.Client, HttpMethod.Post, "/api/accounts", inputJson, showBody: false);
        Console.WriteLine($"Response Content-Type: {response.Content.Headers.ContentType}");
        // The body can be read from the response only once, which is why Fetch was told not to print it.
        Account? created = JsonSerializer.Deserialize<Account>(ReadBody(response), webOptions);
        Console.WriteLine($"Deserialized server reply: {created}");

        // A body that is not valid JSON is refused by the server with 400 (its body is not printed: in
        // Development mode it is a long error report: a stack trace with file paths, and your request headers).
        Send(server.Client, HttpMethod.Post, "/api/accounts", "{\"owner\": }", showBody: false);
    }

    // REST: URLs name THINGS (nouns), the method is the verb, and the status code reports the outcome.
    // Compare /api/accounts/2 (a noun, with GET) with a verb-style URL such as /api/getAccount?id=2 (the
    // action is in the URL, so every new action needs a new URL). REST keeps the URLs as nouns and lets
    // the small set of methods do the verbs.
    public void ExplainRestResources()
    {
        PrintHeading("5. REST resources — nouns in URLs, verbs as methods");

        using RunningServer server = BankApi.Start();
        HttpClient client = server.Client;

        // /api/accounts is the COLLECTION; /api/accounts/2 is one ITEM in it.
        Send(client, HttpMethod.Get, "/api/accounts");
        Send(client, HttpMethod.Get, "/api/accounts/2");

        // Typical status codes: GET 200 (or 404 for a missing item); POST 201 Created plus a Location header
        // that names the new item's URL; PUT 200 (or 404); DELETE 204 (or 404).
        using HttpResponseMessage created = Fetch(client, HttpMethod.Post, "/api/accounts", "{\"owner\":\"Dana Ortiz\",\"balance\":100}");
        string location = created.Headers.Location!.OriginalString;
        Console.WriteLine($"  Location header: {location}");

        // Use the URL the server handed back.
        Send(client, HttpMethod.Get, location);
        Send(client, HttpMethod.Put, location, "{\"owner\":\"Dana Ortiz\",\"balance\":250}");
        Send(client, HttpMethod.Delete, location);
        Send(client, HttpMethod.Get, location);

        // This server only has a route for /api/accounts/{a whole number}, so "abc" matches no route at all.
        Send(client, HttpMethod.Get, "/api/accounts/abc");
    }

    // ---------------------------------------------------------------- helpers

    // Sends one request, prints "METHOD path -> status (family) [body]" and returns the response
    // so the caller can look at headers. The caller must dispose it.
    private static HttpResponseMessage Fetch(HttpClient client, HttpMethod method, string path, string? json = null, bool showBody = true)
    {
        var request = new HttpRequestMessage(method, path);
        if (json != null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, JsonContentType);
        }
        HttpResponseMessage response = client.Send(request);
        PrintExchange(method, path, response, showBody);
        return response;
    }

    // Same, for when only the printed line matters: the response is disposed here.
    private static void Send(HttpClient client, HttpMethod method, string path, string? json = null, bool showBody = true)
    {
        using HttpResponseMessage response = Fetch(client, method, path, json, showBody);
    }

    // A body that is plain text, not JSON: the server answers 415.
    private static void SendPlainText(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts")
        {
            Content = new StringContent("hello", Encoding.UTF8, "text/plain"),
        };
        using HttpResponseMessage response = client.Send(request);
        PrintExchange(request.Method, "/api/accounts (text/plain body)", response, true);
    }

    private static void PrintExchange(HttpMethod method, string path, HttpResponseMessage response, bool showBody)
    {
        string body = showBody ? ReadBody(response) : "";
        int code = (int)response.StatusCode;
        Console.WriteLine($"  {method,-6} {path,-22} -> {code} {response.StatusCode} ({code / 100}xx){(body.Length > 0 ? "  " + body : "")}");
    }

    private static string ReadBody(HttpResponseMessage response)
    {
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }

    private static int CountAccounts(HttpClient client)
    {
        using HttpResponseMessage response = client.Send(new HttpRequestMessage(HttpMethod.Get, "/api/accounts"));
        using JsonDocument document = JsonDocument.Parse(ReadBody(response));
        return document.RootElement.GetArrayLength();
    }

    private static string SendRawText(int port, string text)
    {
        using var tcp = new TcpClient("127.0.0.1", port);
        NetworkStream stream = tcp.GetStream();
        stream.Write(Encoding.ASCII.GetBytes(text));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void PrintIndented(string text)
    {
        foreach (string line in text.TrimEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            if (!line.StartsWith("Date:"))
            {
                Console.WriteLine($"    {line}");
            }
        }
    }

    private static void PrintHeading(string text)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {text} ===");
    }
}
