using Microsoft.EntityFrameworkCore;
using SemanticPoker.Api.Infrastructure.Persistence.Entities;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Infrastructure.Persistence;

public interface IMatchRepository
{
    Task<Match> CreateAsync(Match match, CancellationToken ct = default);
    Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Match>> GetAllAsync(MatchStatus? status = null, CancellationToken ct = default);
    Task UpdateAsync(Match match, CancellationToken ct = default);
    Task<List<LeaderboardEntry>> GetLeaderboardAsync(CancellationToken ct = default);
}

public class MatchRepository : IMatchRepository
{
    private readonly BenchmarkDbContext _db;

    public MatchRepository(BenchmarkDbContext db)
    {
        _db = db;
    }

    public async Task<Match> CreateAsync(Match match, CancellationToken ct = default)
    {
        var entity = EntityMapper.ToEntity(match);
        _db.Matches.Add(entity);
        await _db.SaveChangesAsync(ct);
        return match;
    }

    public async Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Matches
            .Include(m => m.Rounds)
                .ThenInclude(r => r.Sentences)
            .Include(m => m.Rounds)
                .ThenInclude(r => r.PlayerDecisions)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return entity is null ? null : EntityMapper.ToDomain(entity);
    }

    public async Task<List<Match>> GetAllAsync(MatchStatus? status = null, CancellationToken ct = default)
    {
        var query = _db.Matches
            .Include(m => m.Rounds)
            .AsNoTracking()
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        var entities = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return entities.Select(EntityMapper.ToDomain).ToList();
    }

    public async Task UpdateAsync(Match match, CancellationToken ct = default)
    {
        var existing = await _db.Matches
            .Include(m => m.Rounds)
                .ThenInclude(r => r.Sentences)
            .Include(m => m.Rounds)
                .ThenInclude(r => r.PlayerDecisions)
            .FirstOrDefaultAsync(m => m.Id == match.Id, ct);

        if (existing is null) return;

        existing.Status = match.Status;
        existing.CompletedRounds = match.Rounds.Count(r => r.Phase == RoundPhase.Completed);
        existing.ScoresJson = System.Text.Json.JsonSerializer.Serialize(match.Scores);
        existing.StartedAt = match.StartedAt;
        existing.CompletedAt = match.CompletedAt;
        existing.ErrorMessage = match.ErrorMessage;

        // Add new rounds
        foreach (var round in match.Rounds)
        {
            var existingRound = existing.Rounds.FirstOrDefault(r => r.Id == round.Id);
            if (existingRound is null)
            {
                existing.Rounds.Add(EntityMapper.ToEntity(round));
            }
            else
            {
                existingRound.Phase = round.Phase;
                existingRound.CompletedAt = round.CompletedAt;

                // Add new sentences
                foreach (var sentence in round.EngineSentences.Concat(round.ArchitectSentences).Concat(round.ShuffledSentences).DistinctBy(s => s.Id))
                {
                    if (!existingRound.Sentences.Any(s => s.Id == sentence.Id))
                        existingRound.Sentences.Add(EntityMapper.ToEntity(sentence));
                }

                // Add new decisions
                foreach (var decision in round.PlayerDecisions)
                {
                    if (!existingRound.PlayerDecisions.Any(d => d.Id == decision.Id))
                        existingRound.PlayerDecisions.Add(EntityMapper.ToEntity(decision));
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(CancellationToken ct = default)
    {
        var completedMatches = await _db.Matches
            .Where(m => m.Status == MatchStatus.Completed)
            .Include(m => m.Rounds)
                .ThenInclude(r => r.PlayerDecisions)
            .AsNoTracking()
            .ToListAsync(ct);

        var stats = new Dictionary<string, LeaderboardEntry>();

        foreach (var match in completedMatches)
        {
            var modelIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(match.ModelIdsJson) ?? new();

            foreach (var modelId in modelIds)
            {
                if (!stats.ContainsKey(modelId))
                    stats[modelId] = new LeaderboardEntry { ModelId = modelId };

                var entry = stats[modelId];
                entry.TotalMatches++;
            }

            foreach (var round in match.Rounds.Where(r => r.Phase == RoundPhase.Completed))
            {
                var architectId = round.ArchitectModelId;
                if (!stats.ContainsKey(architectId))
                    stats[architectId] = new LeaderboardEntry { ModelId = architectId };

                stats[architectId].RoundsAsArchitect++;

                foreach (var decision in round.PlayerDecisions)
                {
                    if (!stats.ContainsKey(decision.ModelId))
                        stats[decision.ModelId] = new LeaderboardEntry { ModelId = decision.ModelId };

                    var entry = stats[decision.ModelId];

                    if (decision.Role == PlayerRole.Player)
                    {
                        entry.TotalRoundsPlayed++;
                        entry.AvgResponseTimeMs = ((entry.AvgResponseTimeMs * (entry.TotalRoundsPlayed - 1)) + decision.ResponseTimeMs) / entry.TotalRoundsPlayed;

                        switch (decision.DoorOutcome)
                        {
                            case DoorType.Treasure:
                                entry.TreasuresFound++;
                                entry.PlayerScore += decision.ScoreChange;
                                break;
                            case DoorType.Trap:
                                entry.TrapsHit++;
                                entry.PlayerScore += decision.ScoreChange;
                                break;
                            case DoorType.Empty:
                                entry.EmptyDoors++;
                                break;
                        }
                    }
                    else if (decision.Role == PlayerRole.Architect)
                    {
                        entry.ArchitectScore += decision.ScoreChange;
                    }
                }

                // Track architect trap/treasure given away
                var trapCount = round.PlayerDecisions
                    .Where(d => d.Role == PlayerRole.Player && d.DoorOutcome == DoorType.Trap)
                    .Count();
                var treasureCount = round.PlayerDecisions
                    .Where(d => d.Role == PlayerRole.Player && d.DoorOutcome == DoorType.Treasure)
                    .Count();

                stats[architectId].PlayersTrapped += trapCount;
                stats[architectId].TreasuresGivenAway += treasureCount;
            }
        }

        return stats.Values.OrderByDescending(e => e.CombinedScore).ToList();
    }
}
