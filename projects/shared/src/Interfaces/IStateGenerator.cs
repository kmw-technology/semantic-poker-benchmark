using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.Interfaces;

public interface IStateGenerator
{
    GameState Generate(int? seed = null);
}
