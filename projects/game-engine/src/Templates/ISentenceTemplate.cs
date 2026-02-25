using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

public interface ISentenceTemplate
{
    string TemplateId { get; }
    string Description { get; }
    bool IsApplicable(GameState state);
    string Generate(GameState state, Random rng);
}
