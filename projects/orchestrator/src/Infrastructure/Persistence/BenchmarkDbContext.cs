using Microsoft.EntityFrameworkCore;
using SemanticPoker.Api.Infrastructure.Persistence.Entities;

namespace SemanticPoker.Api.Infrastructure.Persistence;

public class BenchmarkDbContext : DbContext
{
    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }

    public DbSet<MatchEntity> Matches => Set<MatchEntity>();
    public DbSet<RoundEntity> Rounds => Set<RoundEntity>();
    public DbSet<SentenceEntity> Sentences => Set<SentenceEntity>();
    public DbSet<PlayerDecisionEntity> PlayerDecisions => Set<PlayerDecisionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MatchEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelIdsJson).HasColumnName("ModelIds");
            entity.Property(e => e.ScoresJson).HasColumnName("Scores");
            entity.HasMany(e => e.Rounds)
                  .WithOne(r => r.Match)
                  .HasForeignKey(r => r.MatchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoundEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GameStateJson).HasColumnName("GameState");
            entity.HasMany(e => e.Sentences)
                  .WithOne(s => s.Round)
                  .HasForeignKey(s => s.RoundId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.PlayerDecisions)
                  .WithOne(d => d.Round)
                  .HasForeignKey(d => d.RoundId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SentenceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<PlayerDecisionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChosenDoor)
                  .HasConversion(v => v.ToString(), v => v[0]);
        });
    }
}
