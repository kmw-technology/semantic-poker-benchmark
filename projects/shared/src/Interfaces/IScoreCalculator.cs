using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.Interfaces;

public interface IScoreCalculator
{
    RoundResult CalculateRound(GameState state, List<PlayerDecision> decisions, string architectModelId);
}
