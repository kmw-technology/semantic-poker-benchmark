using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine;

public class ScoreCalculator : IScoreCalculator
{
    public RoundResult CalculateRound(GameState state, List<PlayerDecision> decisions, string architectModelId)
    {
        var result = new RoundResult
        {
            ArchitectScoreChange = 0,
            PlayerScoreChanges = new Dictionary<string, int>()
        };

        foreach (var decision in decisions)
        {
            var chosenDoor = state.Doors.FirstOrDefault(d => d.Label == decision.ChosenDoor);
            if (chosenDoor == null)
            {
                // Invalid door choice scores 0
                decision.ScoreChange = 0;
                decision.DoorOutcome = DoorType.Empty;
                if (!result.PlayerScoreChanges.ContainsKey(decision.ModelId))
                    result.PlayerScoreChanges[decision.ModelId] = 0;
                continue;
            }

            decision.DoorOutcome = chosenDoor.Type;

            int scoreChange;
            switch (chosenDoor.Type)
            {
                case DoorType.Treasure:
                    scoreChange = 1;
                    result.ArchitectScoreChange -= 1;
                    break;
                case DoorType.Trap:
                    scoreChange = -1;
                    result.ArchitectScoreChange += 1;
                    break;
                case DoorType.Empty:
                default:
                    scoreChange = 0;
                    break;
            }

            decision.ScoreChange = scoreChange;

            if (result.PlayerScoreChanges.ContainsKey(decision.ModelId))
                result.PlayerScoreChanges[decision.ModelId] += scoreChange;
            else
                result.PlayerScoreChanges[decision.ModelId] = scoreChange;
        }

        return result;
    }
}
