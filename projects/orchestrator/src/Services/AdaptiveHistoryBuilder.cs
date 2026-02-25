using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Services;

public class AdaptiveHistoryBuilder
{
    private const int MaxHistoryRounds = 5;

    public List<RoundHistorySummary> BuildArchitectHistory(List<Round> completedRounds)
    {
        return completedRounds
            .Where(r => r.Phase == RoundPhase.Completed)
            .OrderByDescending(r => r.RoundNumber)
            .Take(MaxHistoryRounds)
            .OrderBy(r => r.RoundNumber)
            .Select(r => new RoundHistorySummary
            {
                RoundNumber = r.RoundNumber,
                ArchitectModelId = r.ArchitectModelId,
                PlayerChoices = r.PlayerDecisions
                    .Where(d => d.Role == PlayerRole.Player)
                    .ToDictionary(d => d.ModelId, d => d.ChosenDoor),
                Outcomes = r.PlayerDecisions
                    .Where(d => d.Role == PlayerRole.Player)
                    .ToDictionary(d => d.ModelId, d => d.DoorOutcome.ToString()),
                ShuffledSentenceTexts = r.ShuffledSentences.Select(s => s.Text).ToList()
            })
            .ToList();
    }

    public List<RoundHistorySummary> BuildPlayerHistory(List<Round> completedRounds, string playerModelId)
    {
        return completedRounds
            .Where(r => r.Phase == RoundPhase.Completed)
            .OrderByDescending(r => r.RoundNumber)
            .Take(MaxHistoryRounds)
            .OrderBy(r => r.RoundNumber)
            .Select(r =>
            {
                var playerDecision = r.PlayerDecisions
                    .FirstOrDefault(d => d.ModelId == playerModelId && d.Role == PlayerRole.Player);

                var choices = new Dictionary<string, char>();
                var outcomes = new Dictionary<string, string>();

                if (playerDecision != null)
                {
                    choices["self"] = playerDecision.ChosenDoor;
                    outcomes["self"] = playerDecision.DoorOutcome.ToString();
                }

                return new RoundHistorySummary
                {
                    RoundNumber = r.RoundNumber,
                    ArchitectModelId = r.ArchitectModelId,
                    PlayerChoices = choices,
                    Outcomes = outcomes,
                    ShuffledSentenceTexts = r.ShuffledSentences.Select(s => s.Text).ToList()
                };
            })
            .ToList();
    }
}
