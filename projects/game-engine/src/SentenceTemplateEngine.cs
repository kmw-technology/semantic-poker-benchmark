using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;
using SemanticPoker.GameEngine.Templates;

namespace SemanticPoker.GameEngine;

/// <summary>
/// Implements ISentenceEngine using the template-based sentence generation system.
/// Generates truthful sentences about game state and handles shuffling of
/// engine-generated and architect-generated sentences.
/// </summary>
public class SentenceTemplateEngine : ISentenceEngine
{
    private readonly SentenceTemplateRegistry _registry;

    public SentenceTemplateEngine()
    {
        _registry = new SentenceTemplateRegistry();
    }

    public SentenceTemplateEngine(SentenceTemplateRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Generates a specified number of truthful sentences about the given game state.
    /// Uses the state's Seed for deterministic template selection and generation.
    /// Each sentence comes from a different template to maximize variety.
    /// </summary>
    public List<Sentence> GenerateTrueSentences(GameState state, int count = 2)
    {
        var rng = new Random(state.Seed);
        var applicable = _registry.GetApplicableTemplates(state);

        if (applicable.Count == 0)
        {
            return new List<Sentence>();
        }

        // Shuffle applicable templates and pick up to 'count' unique ones
        var shuffled = applicable.OrderBy(_ => rng.Next()).ToList();
        var selected = shuffled.Take(Math.Min(count, shuffled.Count)).ToList();

        var sentences = new List<Sentence>();
        for (int i = 0; i < selected.Count; i++)
        {
            var template = selected[i];
            var text = template.Generate(state, rng);

            sentences.Add(new Sentence
            {
                Id = Guid.NewGuid(),
                Text = text,
                Source = SentenceSource.Engine,
                IsTruthful = true,
                OriginalIndex = i,
                ShuffledIndex = i, // Will be reassigned during shuffle
                TemplateId = template.TemplateId
            });
        }

        return sentences;
    }

    /// <summary>
    /// Combines engine-generated and architect-generated sentences, shuffles them,
    /// and assigns sequential ShuffledIndex values.
    /// </summary>
    public List<Sentence> ShuffleSentences(List<Sentence> engine, List<Sentence> architect, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        var combined = new List<Sentence>();
        combined.AddRange(engine);
        combined.AddRange(architect);

        // Assign original indices before shuffling
        for (int i = 0; i < combined.Count; i++)
        {
            combined[i].OriginalIndex = i;
        }

        // Fisher-Yates shuffle for uniform randomness
        for (int i = combined.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (combined[i], combined[j]) = (combined[j], combined[i]);
        }

        // Assign shuffled indices
        for (int i = 0; i < combined.Count; i++)
        {
            combined[i].ShuffledIndex = i;
        }

        return combined;
    }
}
