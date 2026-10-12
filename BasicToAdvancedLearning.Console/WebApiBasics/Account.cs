using System.ComponentModel.DataAnnotations;

namespace BasicToAdvancedLearning.WebApiBasics;

// The bank account the lesson's API talks about. A plain class with public properties: ASP.NET Core turns
// it into JSON (and back) using those property names. In-memory only; there is no database in this topic.
public class Account
{
    public int AccountId { get; set; }
    public string AccountNumber { get; set; } = "";
    public string AccountType { get; set; } = "";   // "Savings" or "Current"
    public decimal Balance { get; set; }
}

// What a client sends in the BODY of POST /api/accounts to open a new account.
public class NewAccountRequest
{
    public string AccountNumber { get; set; } = "";
    public string AccountType { get; set; } = "";
}

// What a client sends in the BODY of a deposit. The [Range] attribute is a validation rule: with
// [ApiController], a body that breaks it is answered with 400 before the action even runs.
public class DepositRequest
{
    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Amount { get; set; }
}
