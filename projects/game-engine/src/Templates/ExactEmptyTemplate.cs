using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "Door {X} is Empty."
/// Only for doors that are actually Empty.
/// </summary>
public class ExactEmptyTemplate : ISentenceTemplate
{
    public string TemplateId => "exact-empty";
    public string Description => "States that a specific door is Empty.";

    public bool IsApplicable(GameState state)
    {
        // Applicable when there is at least one Empty door.
        return state.Doors.Any(d => d.Type == DoorType.Empty);
    }

    public string Generate(GameState state, Random rng)
    {
        var emptyDoors = state.Doors.Where(d => d.Type == DoorType.Empty).ToList();
        var picked = emptyDoors[rng.Next(emptyDoors.Count)];

        return $"Door {picked.Label} is Empty.";
    }
}
