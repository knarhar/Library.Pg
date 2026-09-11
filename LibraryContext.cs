using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Library.Pg
{
    internal class LibraryContext : DbContext
    {
        private const string Cs = "Host=127.0.0.1;Port=5433;Username=postgres;Password=admin;Database=AppPgDb";
        public DbSet<Book> Books => Set<Book>();
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(Cs);
            options.LogTo(
                   Console.WriteLine,
                   new[] { DbLoggerCategory.Database.Command.Name },
                   LogLevel.Information,
                   DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime);

            options.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable("books");

                entity.Property(b => b.Title).IsRequired().HasMaxLength(200);
                entity.Property(b => b.Author).IsRequired().HasMaxLength(120);
                entity.Property(b => b.Price).HasPrecision(10, 2);
                entity.HasIndex(b => b.Author);
                entity.Property(b => b.AddedAt).HasDefaultValue(DateTime.UtcNow);
            });
        }
    }
}
