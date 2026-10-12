using System.Security.Cryptography;
using System.Text;

namespace BasicToAdvancedLearning.JwtAuth;

// Topic 20: Basic authentication and authorization with JWT (JSON Web Tokens). The running example is the
// bank's API with two pretend users: asha (a Customer) and chen (a Manager). Each method starts a small
// in-process copy of that API (a real API is a separate program that keeps running; WebLab hosts it inside
// this console program only so the lesson can finish) and makes REAL HTTP calls to it.
//
// Not built here, only worth knowing about (each is a topic of its own):
//   - Passwords: a real API stores a salted hash made by a slow password algorithm (PBKDF2, bcrypt, Argon2).
//   - HTTPS: a token is a "key to the door" - anyone who copies it can use it until it expires. Send it only
//     over HTTPS, so nobody on the network path can read it.
//   - Refresh tokens: short-lived access tokens plus a longer-lived refresh token let a client stay logged in
//     without a long-lived access token.
//   - Secrets: the signing key is never written in source code; this lesson makes a new random one per run.
// Tokens and keys are random or time-based, so the lesson prints only things that are the same on every run
// (status codes, field names, fixed values, True/False) - never a token or any part of one.
// See ArchitectureDiagrams/JwtAuth for the visual walkthrough.
public class JwtAuthDemo
{
    // 401 and 403 are the two answers this whole lesson is about.
    public void ExplainAuthenticationVsAuthorization()
    {
        PrintHeading("1. Authentication vs authorization - 401 and 403");

        // Authentication = "who are you?" (prove your identity).  Authorization = "are you allowed to do this?"
        // The server answers 401 Unauthorized when it does not know who you are (no token, or a bad one) and
        // 403 Forbidden when it knows exactly who you are but the answer is "not you". (The 401 name says
        // "Unauthorized", but it really means "not authenticated" - a long-standing quirk of the HTTP name.)
        using (DemoAuthServer server = DemoAuthServer.Start(null))
        {
            string customerToken = ApiCall.Login(server.Client, "asha");
            string managerToken = ApiCall.Login(server.Client, "chen");

            ApiReply noToken = ApiCall.Get(server.Client, "/api/reports/daily");
            ApiReply wrongRole = ApiCall.Get(server.Client, "/api/reports/daily", customerToken);
            ApiReply rightRole = ApiCall.Get(server.Client, "/api/reports/daily", managerToken);

            Console.WriteLine($"  GET /api/reports/daily  no token              -> {noToken.Summary}   (who are you?)");
            Console.WriteLine($"  GET /api/reports/daily  asha (a Customer)     -> {wrongRole.Summary}      (I know you - but not you)");
            Console.WriteLine($"  GET /api/reports/daily  chen (a Manager)      -> {rightRole.Summary}");
        }
    }

    // A JWT is not magic: it is three pieces of text joined by dots. We take one apart by hand.
    public void ExplainWhatIsAJwt()
    {
        PrintHeading("2. What is a JWT - three parts, readable by anyone, protected from changes");

        using (DemoAuthServer server = DemoAuthServer.Start(null))
        {
            string token = ApiCall.Login(server.Client, "asha");

            // header.payload.signature - each piece is base64url text. We never print the token itself.
            string[] parts = token.Split('.');
            Console.WriteLine($"  The token has {parts.Length} parts joined by dots: header . payload . signature");

            // Decoding is NOT decryption: no key is used here, so anyone holding the token can read it.
            string headerJson = TokenParts.DecodeText(parts[0]);
            string payloadJson = TokenParts.DecodeText(parts[1]);
            Console.WriteLine($"  header fields : {TokenParts.FieldNames(headerJson)}");
            Console.WriteLine($"  header says   : alg = {TokenParts.GetText(headerJson, "alg")}, typ = {TokenParts.GetText(headerJson, "typ")}");
            Console.WriteLine($"  payload fields: {TokenParts.FieldNames(payloadJson)}");
            Console.WriteLine($"  payload says  : sub = {TokenParts.GetText(payloadJson, "sub")}, role = {TokenParts.GetText(payloadJson, "role")}  (read WITHOUT any key)");
            Console.WriteLine("  so never put passwords or other secrets in claims.");

            // The signature is a fingerprint of "header.payload" made with the secret key (HMAC-SHA256).
            // The lesson and the server share one process, so we can reach the key and rebuild the signature
            // ourselves. A real client never has the key - only the server that signs and checks.
            byte[] signed = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
            byte[] rebuilt = HMACSHA256.HashData(server.Issuer.SigningKeyBytes, signed);
            bool sameSignature = TokenParts.Encode(rebuilt) == parts[2];
            Console.WriteLine($"  signature is {TokenParts.DecodeBytes(parts[2]).Length} bytes; rebuilt from header.payload + key matches: {sameSignature}");

            // Tamper: change the role inside the payload, keep the old signature. The signature no longer
            // fits the payload, so the server rejects the token. Signed means "unchanged", not "secret".
            string forgedPayload = payloadJson.Replace("\"role\":\"Customer\"", "\"role\":\"Manager\"");
            string forgedToken = parts[0] + "." + TokenParts.EncodeText(forgedPayload) + "." + parts[2];
            ApiReply genuine = ApiCall.Get(server.Client, "/api/reports/daily", token);
            ApiReply forged = ApiCall.Get(server.Client, "/api/reports/daily", forgedToken);
            Console.WriteLine($"  payload really changed: {forgedPayload != payloadJson}");
            Console.WriteLine($"  genuine token (Customer) -> {genuine.Summary}   (token accepted; a Customer may not see the report)");
            Console.WriteLine($"  forged token (says Manager, old signature) -> {forged.Summary}   (token itself refused)");
        }
    }

    // POST /login: check the name and password, and if they are right hand back a signed token.
    public void ExplainIssuingATokenLogin()
    {
        PrintHeading("3. Logging in - POST /login returns a signed token");

        using (DemoAuthServer server = DemoAuthServer.Start(null))
        {
            // A wrong name and a wrong password get the same answer, so a caller cannot tell which was wrong.
            ApiReply wrongPassword = ApiCall.PostLogin(server.Client, "asha", "not-the-password");
            ApiReply unknownUser = ApiCall.PostLogin(server.Client, "nobody", DemoUsers.DemoPassword);
            ApiReply success = ApiCall.PostLogin(server.Client, "asha", DemoUsers.DemoPassword);
            Console.WriteLine($"  POST /login asha + wrong password   -> {wrongPassword.Summary}");
            Console.WriteLine($"  POST /login nobody + any password   -> {unknownUser.Summary}");
            Console.WriteLine($"  POST /login asha + right password   -> {success.Summary}");

            // The reply body is JSON with one field, "token". Inside the token's payload are the claims
            // (facts about the user) that the server wrote when it signed it.
            string token = ApiCall.Login(server.Client, "asha");
            string payloadJson = TokenParts.DecodeText(token.Split('.')[1]);
            long lifetimeSeconds = TokenParts.GetNumber(payloadJson, "exp") - TokenParts.GetNumber(payloadJson, "nbf");
            Console.WriteLine($"  claims: sub = {TokenParts.GetText(payloadJson, "sub")}, name = {TokenParts.GetText(payloadJson, "name")}, role = {TokenParts.GetText(payloadJson, "role")}");
            Console.WriteLine($"  iss = {TokenParts.GetText(payloadJson, "iss")} (who made it), aud = {TokenParts.GetText(payloadJson, "aud")} (who it is for)");

            // exp and nbf are NumericDate values: whole seconds since 1970-01-01 UTC.
            Console.WriteLine($"  exp - nbf = {lifetimeSeconds} seconds ({lifetimeSeconds / 60} minutes of life)");
        }
    }

    // [Authorize], [AllowAnonymous] and [Authorize(Roles = ...)] - plus how the client sends the token.
    public void ExplainProtectingEndpoints()
    {
        PrintHeading("4. Protecting endpoints - [Authorize], [AllowAnonymous], Roles");

        using (DemoAuthServer server = DemoAuthServer.Start(null))
        {
            string customerToken = ApiCall.Login(server.Client, "asha");
            string managerToken = ApiCall.Login(server.Client, "chen");

            // The client puts the token in a header of every request:   Authorization: Bearer <token>
            // ("Bearer" = whoever bears - carries - this token is treated as its owner.)
            ApiReply open = ApiCall.Get(server.Client, "/api/status");
            ApiReply anonymousToMe = ApiCall.Get(server.Client, "/api/accounts/me");
            ApiReply customerToMe = ApiCall.Get(server.Client, "/api/accounts/me", customerToken);
            ApiReply customerToReport = ApiCall.Get(server.Client, "/api/reports/daily", customerToken);
            ApiReply managerToReport = ApiCall.Get(server.Client, "/api/reports/daily", managerToken);
            ApiReply garbage = ApiCall.Get(server.Client, "/api/accounts/me", "this-is-not-a-token");
            ApiReply wrongScheme = ApiCall.Get(server.Client, "/api/accounts/me", customerToken, "Basic");

            Console.WriteLine($"  GET /api/status          [AllowAnonymous]            no token        -> {open.Summary}: {open.Body}");
            Console.WriteLine($"  GET /api/accounts/me     [Authorize]                 no token        -> {anonymousToMe.Summary}");
            Console.WriteLine($"  GET /api/accounts/me     [Authorize]                 asha's token    -> {customerToMe.Summary}: {customerToMe.Body}");
            Console.WriteLine($"  GET /api/reports/daily   [Authorize(Roles=Manager)]  asha's token    -> {customerToReport.Summary}");
            Console.WriteLine($"  GET /api/reports/daily   [Authorize(Roles=Manager)]  chen's token    -> {managerToReport.Summary}: {managerToReport.Body}");
            Console.WriteLine($"  GET /api/accounts/me     [Authorize]                 \"Bearer <junk>\" -> {garbage.Summary}");
            Console.WriteLine($"  GET /api/accounts/me     [Authorize]                 \"Basic <token>\" -> {wrongScheme.Summary}");

            // A 401 from the JWT handler also carries a WWW-Authenticate header naming the scheme to use.
            Console.WriteLine($"  the 401 for 'no token' names the scheme to use (WWW-Authenticate starts with Bearer): {anonymousToMe.Challenge.StartsWith("Bearer")}");
        }
    }

    // exp is the end of a token's life. ClockSkew is how much extra time the server allows after exp.
    public void ExplainTokenLifetime()
    {
        PrintHeading("5. Token lifetime - exp, expired tokens and ClockSkew");

        // Why expire at all? A signed token cannot be "taken back" by the server on its own: it works until exp
        // unless the server keeps extra state (a block list). A short life limits the damage of a stolen token.
        //
        // ClockSkew: two computers' clocks are never exactly equal, so the library allows some slack after exp.
        // We set it explicitly instead of trusting a default: zero for the strict server, 5 minutes for the lenient one.
        // (The library's own default, when you do not set it, is also 5 minutes - an expired token is accepted
        // for a few minutes more. Many APIs lower it, because a short life is part of the protection.)
        DateTime now = DateTime.UtcNow;
        DemoUser asha = new DemoUser("asha", "Asha Rao", DemoUsers.CustomerRole);

        // Two servers at once: the strict one (no slack) and the lenient one (5 minutes of slack).
        using (DemoAuthServer strict = DemoAuthServer.Start(TimeSpan.Zero))
        using (DemoAuthServer lenient = DemoAuthServer.Start(TimeSpan.FromMinutes(5)))
        {
            // Issue tokens by hand with times in the past, so we do not have to wait for a real expiry.
            // (Each server has its own key, so each server needs tokens signed by its own issuer.)
            string fresh = strict.Issuer.Issue(asha, now, now.AddMinutes(TokenIssuer.LoginLifetimeMinutes));
            string expired = strict.Issuer.Issue(asha, now.AddMinutes(-10), now.AddMinutes(-1));   // ended 1 minute ago

            ApiReply freshReply = ApiCall.Get(strict.Client, "/api/accounts/me", fresh);
            ApiReply expiredReply = ApiCall.Get(strict.Client, "/api/accounts/me", expired);
            Console.WriteLine($"  ClockSkew = 0 min:  fresh token (expires in 15 min)  -> {freshReply.Summary}");
            Console.WriteLine($"  ClockSkew = 0 min:  token that expired 1 min ago     -> {expiredReply.Summary}");
            Console.WriteLine($"  the 401 says why (WWW-Authenticate mentions invalid_token): {expiredReply.Challenge.Contains("invalid_token")}");

            string expiredOneMinute = lenient.Issuer.Issue(asha, now.AddMinutes(-10), now.AddMinutes(-1));
            string expiredTenMinutes = lenient.Issuer.Issue(asha, now.AddMinutes(-20), now.AddMinutes(-10));

            ApiReply withinSkew = ApiCall.Get(lenient.Client, "/api/accounts/me", expiredOneMinute);
            ApiReply beyondSkew = ApiCall.Get(lenient.Client, "/api/accounts/me", expiredTenMinutes);
            Console.WriteLine($"  ClockSkew = 5 min:  token that expired 1 min ago     -> {withinSkew.Summary}   (inside the 5 minute slack)");
            Console.WriteLine($"  ClockSkew = 5 min:  token that expired 10 min ago    -> {beyondSkew.Summary}");
        }
    }

    // Prints a title line for each lesson method.
    private static void PrintHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }
}
