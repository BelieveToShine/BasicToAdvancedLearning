namespace BasicToAdvancedLearning.JwtAuth;

// One pretend bank employee or customer. Roles are plain text here ("Customer", "Manager").
public sealed record DemoUser(string UserName, string FullName, string Role);

// The "user database" of this lesson: two people in a list in memory. A real application keeps users in a
// database and stores only a salted HASH of each password (made with a slow algorithm built for passwords,
// such as PBKDF2, bcrypt or Argon2), never the password itself. This lesson does not build that - it is a
// topic of its own. The shared password below is a made-up value for the demo, not a real secret.
public sealed class DemoUsers
{
    public const string DemoPassword = "demo-password-not-real";
    public const string CustomerRole = "Customer";
    public const string ManagerRole = "Manager";

    // An instance list (not static): the server creates one DemoUsers and shares it with the controller.
    private readonly DemoUser[] _users =
    {
        new DemoUser("asha", "Asha Rao", CustomerRole),
        new DemoUser("chen", "Chen Li", ManagerRole),
    };

    // Returns the user when the name and password match, otherwise null. Callers should answer a wrong
    // name and a wrong password the same way, so a caller cannot tell which one was wrong from the status and body (response timing can still differ, because an unknown user is rejected before any password work).
    public DemoUser? CheckLogin(string userName, string password)
    {
        DemoUser? user = _users.FirstOrDefault(u => u.UserName == userName);
        // DEMO ONLY: this compares the typed password with one shared plain-text constant using ==. Real code
        // never does that: it loads the user's stored salted hash and calls the password-hashing library's
        // verify method (PBKDF2/bcrypt/Argon2), which also compares in constant time.
        return user != null && password == DemoPassword ? user : null;
    }
}
