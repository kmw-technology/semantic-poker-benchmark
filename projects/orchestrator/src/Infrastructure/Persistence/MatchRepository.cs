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
        // Clear change tracker to avoid stale entity conflicts from repeated calls within same scope
        _db.ChangeTracker.Clear();

        // Update match-level fields directly
        var matchEntity = await _db.Matches.FindAsync(new object[] { match.Id }, ct);
        if (matchEntity is null) return;

        matchEntity.Status = match.Status;
        matchEntity.CompletedRounds = match.Rounds.Count(r => r.Phase == RoundPhase.Completed);
        matchEntity.ScoresJson = System.Text.Json.JsonSerializer.Serialize(match.Scores);
        matchEntity.StartedAt = match.StartedAt;
        matchEntity.CompletedAt = match.CompletedAt;
        matchEntity.ErrorMessage = match.ErrorMessage;

        // Sync rounds
        var existingRoundIds = await _db.Rounds
            .Where(r => r.MatchId == match.Id)
            .Select(r => r.Id)
            .ToListAsync(ct);

        foreach (var round in match.Rounds)
        {
            if (existingRoundIds.Contains(round.Id))
            {
                // Update existing round
                var existingRound = await _db.Rounds.FindAsync(new object[] { round.Id }, ct);
                if (existingRound != null)
                {
                    existingRound.Phase = round.Phase;
                    existingRound.CompletedAt = round.CompletedAt;
                }

                // Add new sentences
                var existingSentenceIds = await _db.Sentences
                    .Where(s => s.RoundId == round.Id)
                    .Select(s => s.Id)
                    .ToListAsync(ct);

                var allSentences = round.EngineSentences
                    .Concat(round.ArchitectSentences)
                    .Concat(round.ShuffledSentences)
                    .DistinctBy(s => s.Id);

                foreach (var sentence in allSentences)
                {
                    if (!existingSentenceIds.Contains(sentence.Id))
                    {
                        var entity = EntityMapper.ToEntity(sentence);
                        entity.RoundId = round.Id;
                        _db.Sentences.Add(entity);
                    }
                }

                // Add new decisions
                var existingDecisionIds = await _db.PlayerDecisions
                    .Where(d => d.RoundId == round.Id)
                    .Select(d => d.Id)
                    .ToListAsync(ct);

                foreach (var decision in round.PlayerDecisions)
                {
                    if (!existingDecisionIds.Contains(decision.Id))
                    {
                        var entity = EntityMapper.ToEntity(decision);
                        entity.RoundId = round.Id;
                        _db.PlayerDecisions.Add(entity);
                    }
                    else
                    {
                        // Update score changes on existing decisions
                        var existingDecision = await _db.PlayerDecisions.FindAsync(new object[] { decision.Id }, ct);
                        if (existingDecision != null)
                            existingDecision.ScoreChange = decision.ScoreChange;
                    }
                }
            }
            else
            {
                // Add new round with all children
                var roundEntity = EntityMapper.ToEntity(round);
                roundEntity.MatchId = match.Id;
                _db.Rounds.Add(roundEntity);
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
