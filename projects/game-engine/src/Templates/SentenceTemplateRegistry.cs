using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Registers all sentence templates and provides methods to query applicable templates
/// for a given game state.
/// </summary>
public class SentenceTemplateRegistry
{
    private readonly List<ISentenceTemplate> _templates;

    public SentenceTemplateRegistry()
    {
        _templates = new List<ISentenceTemplate>
        {
            new NegativeDoorTemplate(),
            new PairExclusionTemplate(),
            new TripletContainsTemplate(),
            new PositionalRelationTemplate(),
            new AdjacentDoorTemplate(),
            new SameSideTemplate(),
            new GapTemplate(),
            new EndpointTemplate(),
            new MiddleDoorTemplate(),
            new EmptyNeighborTemplate(),
            new OrderTemplate(),
            new ExactEmptyTemplate(),
            new QualitativeDistanceTemplate()
        };
    }

    /// <summary>
    /// Returns all registered templates.
    /// </summary>
    public IReadOnlyList<ISentenceTemplate> AllTemplates => _templates.AsReadOnly();

    /// <summary>
    /// Returns only templates that are applicable for the given game state.
    /// </summary>
    public List<ISentenceTemplate> GetApplicableTemplates(GameState state)
    {
        return _templates.Where(t => t.IsApplicable(state)).ToList();
    }

    /// <summary>
    /// Returns a specific template by its ID, or null if not found.
    /// </summary>
    public ISentenceTemplate? GetTemplateById(string templateId)
    {
        return _templates.FirstOrDefault(t => t.TemplateId == templateId);
    }
}
