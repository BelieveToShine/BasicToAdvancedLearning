using Microsoft.EntityFrameworkCore;

namespace FullStackBank.Api.Data;

public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<BankTransaction> Transactions => Set<BankTransaction>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQL Server's DATETIME2 does not store "this is UTC". EF would hand it back with Kind = Unspecified and
        // System.Text.Json would then write "2026-01-01T10:00:00" with no "Z", which a browser reads as LOCAL time.
        // Marking every DateTime as UTC on the way out makes the JSON end with "Z". We only ever store UtcNow.
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.Property(c => c.FullName).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(c => c.Email).IsUnique();
            e.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("Accounts", t =>
            {
                // CHECK constraints are the database's last line of defence: even a bug in the C# code (or a
                // hand-written UPDATE) cannot store an unknown account type or a negative balance.
                t.HasCheckConstraint("CK_Accounts_AccountType", "[AccountType] IN ('Savings','Current')");
                t.HasCheckConstraint("CK_Accounts_Balance_NotNegative", "[Balance] >= 0");
            });
            e.Property(a => a.AccountNumber).HasMaxLength(20).IsRequired();
            e.HasIndex(a => a.AccountNumber).IsUnique();
            // Stored as text ('Savings'), not 0/1: readable in SSMS and safe if the enum is ever reordered.
            e.Property(a => a.AccountType).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Balance).HasPrecision(18, 2).HasDefaultValue(0m);
            // Restrict = the database refuses to delete a customer who still has accounts (no silent cascade of money data).
            e.HasOne(a => a.Customer).WithMany(c => c.Accounts).HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BankTransaction>(e =>
        {
            e.ToTable("Transactions", t =>
            {
                t.HasCheckConstraint("CK_Transactions_Kind", "[Kind] IN ('Deposit','Withdrawal')");
                t.HasCheckConstraint("CK_Transactions_Amount_Positive", "[Amount] > 0");
            });
            // Not found by convention: the class is BankTransaction, so EF would look for "BankTransactionId".
            e.HasKey(x => x.TransactionId);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.Account).WithMany(a => a.Transactions).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

// Reads every DateTime from SQL Server as UTC (see ConfigureConventions above).
public class UtcDateTimeConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
