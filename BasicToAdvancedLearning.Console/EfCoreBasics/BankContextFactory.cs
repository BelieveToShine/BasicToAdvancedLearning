using BasicToAdvancedLearning.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BasicToAdvancedLearning.EfCoreBasics;

// Used only by the dotnet-ef command-line tool (for example `dotnet ef migrations add`). The tool
// cannot run this lesson's Main, so it looks for a class like this one and asks it for a BankContext.
// Writing a migration only needs the MODEL, not a live database, so a placeholder connection string
// is enough when BTAL_SQLSERVER is not set.
public class BankContextFactory : IDesignTimeDbContextFactory<BankContext>
{
    private const string PlaceholderConnectionString =
        "Server=localhost;Database=EfCoreBasicsDesignTime;Integrated Security=True;TrustServerCertificate=True";

    public BankContext CreateDbContext(string[] args)
    {
        string connectionString = SqlLab.ConnectionString ?? PlaceholderConnectionString;
        DbContextOptions<BankContext> options = new DbContextOptionsBuilder<BankContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new BankContext(options);
    }
}
