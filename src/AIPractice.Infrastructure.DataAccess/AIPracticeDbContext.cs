using AIPractice.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace AIPractice.Infrastructure.Data;

public class AIPracticeDbContext : DbContext
{
    public AIPracticeDbContext(DbContextOptions<AIPracticeDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AIPracticeDbContext).Assembly);
    }
}