using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.Interfaces;

public interface ISentenceEngine
{
    List<Sentence> GenerateTrueSentences(GameState state, int count = 2);
    List<Sentence> ShuffleSentences(List<Sentence> engine, List<Sentence> architect, int? seed = null);
}
