using Microsoft.EntityFrameworkCore;

namespace BasicToAdvancedLearning.WebApiCrud;

// The EF Core "unit of work": it knows the tables (the DbSet properties), tracks the entities you load
// or add, and sends one SaveChanges() as SQL. It is cheap to create and meant to be SHORT-LIVED —
// which is exactly why the web app asks DI for a fresh one per request (see ExplainDbContextInDI).
public class BankDbContext : DbContext
{
    // DI hands over the options (which database, which provider) — the context never reads a
    // connection string itself.
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<BankTransaction> Transactions => Set<BankTransaction>();

    // Describes the schema so that EnsureCreated() builds the same tables as the SQL lessons.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.Property(c => c.FullName).HasMaxLength(100);
            customer.Property(c => c.Email).HasMaxLength(200);
            customer.HasIndex(c => c.Email).IsUnique();
            customer.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Account>(account =>
        {
            account.Property(a => a.AccountNumber).HasMaxLength(20);
            account.Property(a => a.AccountType).HasMaxLength(20);
            account.Property(a => a.Balance).HasPrecision(18, 2);
            account.HasIndex(a => a.AccountNumber).IsUnique();
            account.ToTable(t => t.HasCheckConstraint("CK_Accounts_AccountType", "[AccountType] IN ('Savings','Current')"));

            // Restrict = the database refuses to delete a customer who still has accounts. EF's default for a
            // required relationship would be Cascade (the database silently deletes the children too).
            account.HasOne(a => a.Customer).WithMany(c => c.Accounts).HasForeignKey(a => a.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
            account.HasMany(a => a.Transactions).WithOne().HasForeignKey(t => t.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BankTransaction>(transaction =>
        {
            // The class is BankTransaction, so EF cannot guess that "TransactionId" is the key: say so.
            transaction.HasKey(t => t.TransactionId);
            transaction.ToTable("Transactions");
            transaction.Property(t => t.Amount).HasPrecision(18, 2);
            transaction.Property(t => t.Kind).HasMaxLength(10);
            transaction.Property(t => t.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            transaction.ToTable(t => t.HasCheckConstraint("CK_Transactions_Kind", "[Kind] IN ('Deposit','Withdrawal')"));
        });
    }
}
