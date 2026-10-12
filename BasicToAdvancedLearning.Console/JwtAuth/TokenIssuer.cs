using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BasicToAdvancedLearning.JwtAuth;

// Builds and signs tokens. The signing key is the one secret in this whole lesson, so it is created fresh
// from random bytes every time a server starts - it is never written in the source code. A real API reads
// its key from a secret store or an environment variable (and keeps it out of source control), because
// anyone who knows the key can mint tokens that the server will accept.
public sealed class TokenIssuer
{
    public const string Issuer = "BtalBank";           // who made the token
    public const string Audience = "BtalBankApi";      // who the token is meant for
    public const int LoginLifetimeMinutes = 15;        // how long a token from /login stays valid
    private const int KeyBytes = 32;                   // 256 bits: the RFC for JWT algorithms asks for a key at least this long for HS256

    public TokenIssuer()
    {
        SigningKeyBytes = RandomNumberGenerator.GetBytes(KeyBytes);
        Key = new SymmetricSecurityKey(SigningKeyBytes);
    }

    public byte[] SigningKeyBytes { get; }
    public SymmetricSecurityKey Key { get; }

    // Creates a signed token for a user, valid from notBefore until expires. /login uses "now" and "now + 15
    // minutes". The lesson also calls it directly with times in the past, to get an expired token without waiting.
    public string Issue(DemoUser user, DateTime notBeforeUtc, DateTime expiresUtc)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            // Claims = facts about the user, written into the token: sub (subject = who), name, role. The handler also
            // adds iss/aud (who made it / who it is for), nbf (not before), exp (expires) and iat (issued at = when it was made).
            // Never put passwords or other secrets in claims: anyone can read them.
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.UserName),
                new Claim("name", user.FullName),
                new Claim("role", user.Role),
            }),
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = notBeforeUtc,
            NotBefore = notBeforeUtc,
            Expires = expiresUtc,
            // HS256 = HMAC with SHA-256: one shared secret key both signs and checks.
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
