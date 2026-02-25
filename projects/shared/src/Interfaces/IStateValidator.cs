using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.Interfaces;

public interface IStateValidator
{
    bool IsValidGameState(GameState state);
    List<string> GetValidationErrors(GameState state);
}
