using Microsoft.EntityFrameworkCore;
using BasicToAdvancedLearning.Interfaces;

namespace BasicToAdvancedLearning.EfCoreCrud;

// A DbContext is the object you work through: it holds the connection settings, one DbSet per table, and the
// CHANGE TRACKER (the list of entity objects it is watching, with the state of each). Create one, use it for
// a small unit of work, dispose it. It is not meant to live for the whole program.
public class BankContext : DbContext
{
    private readonly SqlSpy? _spy;

    public BankContext(SqlSpy? spy = null)
    {
        _spy = spy;
    }

    // One DbSet per table: this is the starting point of every query ("from Customers ...").
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<BankTransaction> Transactions => Set<BankTransaction>();

    // Connection string comes from the environment variable (never from source) via SqlLab (Topic 10).
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(SqlLab.ConnectionString);
        if (_spy != null)
        {
            options.AddInterceptors(_spy);   // lets SqlSpy see every SQL command EF sends
        }
    }

    // Describes the tables to match the bank schema used in the SQL topics.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.Property(c => c.FullName).HasMaxLength(100);
            e.Property(c => c.Email).HasMaxLength(200);
            e.HasIndex(c => c.Email).IsUnique();
            e.Property(c => c.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("Accounts", t => t.HasCheckConstraint("CK_Accounts_AccountType", "[AccountType] IN ('Savings','Current')"));
            e.Property(a => a.AccountNumber).HasMaxLength(20);
            e.HasIndex(a => a.AccountNumber).IsUnique();
            e.Property(a => a.AccountType).HasMaxLength(20);
            e.Property(a => a.Balance).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

            // Deleting a customer who still has accounts must be REFUSED by the database (see RemovingRows).
            // Restrict = the foreign key has no ON DELETE action, so SQL Server blocks the delete.
            e.HasOne(a => a.Customer).WithMany(c => c.Accounts).HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BankTransaction>(e =>
        {
            e.ToTable("Transactions", t => t.HasCheckConstraint("CK_Transactions_Kind", "[Kind] IN ('Deposit','Withdrawal')"));
            e.HasKey(t => t.TransactionId);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Kind).HasMaxLength(10);
            e.Property(t => t.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");

            // Deleting an account also deletes its transactions: the database does it (ON DELETE CASCADE).
            e.HasOne(t => t.Account).WithMany(a => a.Transactions).HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
