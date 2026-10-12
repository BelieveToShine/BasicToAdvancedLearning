using BasicToAdvancedLearning.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BasicToAdvancedLearning.EfCoreBasics;

// A DbContext is the object you talk to EF Core through. It holds the MODEL (which classes map to
// which tables) and, once you query or save (Topic 15), a database connection and a record of the
// objects it has loaded. This first one has NO extra configuration: every table and column comes
// from EF Core's built-in conventions, so method 2 can show exactly what those conventions produce.
public class PlainBankContext : DbContext
{
    // A DbSet<T> is the door to one table. The property NAME is what EF Core uses as the table name.
    public DbSet<Customer> Customers { get; set; } = null!; // EF Core fills this in when the context is created

    // Account is reachable from Customer.Accounts anyway; listing it here as well gives its table a
    // name that matches the SQL topics (Accounts) and lets you query it directly.
    public DbSet<Account> Accounts { get; set; } = null!;

    // Says which database engine and which database to use. Nothing connects here; this only stores
    // the settings. The connection string is read from the BTAL_SQLSERVER environment variable
    // (never written in source code).
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(SqlLab.ConnectionString);
    }
}

// The same two tables, now configured with the Fluent API (sizes, unique indexes, delete behaviour; no defaults or CHECK rules). Used by methods 3-5 and by the migration.
public class BankContext : DbContext
{
    public BankContext()
    {
    }

    // The second constructor lets the code that calls it supply the settings. The dotnet-ef
    // command-line tool uses this one through BankContextFactory (see that file).
    public BankContext(DbContextOptions<BankContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;

    public DbSet<Account> Accounts { get; set; } = null!;

    // Only fill in the settings if the caller has not already supplied them.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(SqlLab.ConnectionString);
        }
    }

    // The Fluent API: C# method calls that refine what the conventions decided. EF Core runs this once,
    // the first time it needs the model, and keeps the result.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.Property(c => c.FullName).IsRequired().HasMaxLength(100);
            customer.Property(c => c.Email).IsRequired().HasMaxLength(200);
            customer.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Account>(account =>
        {
            account.Property(a => a.AccountNumber).HasMaxLength(20);
            account.HasIndex(a => a.AccountNumber).IsUnique();
            account.Property(a => a.AccountType).HasMaxLength(20);
            account.Property(a => a.Balance).HasPrecision(18, 2);

            // One customer has many accounts; an account cannot exist without its customer.
            // Restrict = SQL Server refuses to delete a customer who still has accounts.
            account.HasOne(a => a.Customer)
                   .WithMany(c => c.Accounts)
                   .HasForeignKey(a => a.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
