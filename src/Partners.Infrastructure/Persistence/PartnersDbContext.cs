using Microsoft.EntityFrameworkCore;

namespace Partners.Infrastructure.Persistence;

public sealed class PartnersDbContext(DbContextOptions<PartnersDbContext> options) : DbContext(options)
{
    public DbSet<PartnerModel> Partners => Set<PartnerModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartnersDbContext).Assembly);
}
