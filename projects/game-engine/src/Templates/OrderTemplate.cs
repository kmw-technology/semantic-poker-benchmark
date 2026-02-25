using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The Treasure comes {before/after} the Trap in alphabetical door order."
/// Compares the indices (alphabetical positions) of the Treasure and Trap doors.
/// </summary>
public class OrderTemplate : ISentenceTemplate
{
    public string TemplateId => "order";
    public string Description => "States whether the Treasure comes before or after the Trap in alphabetical order.";

    public bool IsApplicable(GameState state)
    {
        // Always applicable when both Treasure and Trap exist and are on different doors.
        return state.Doors.Count == 5
               && state.Doors.Any(d => d.Type == DoorType.Treasure)
               && state.Doors.Any(d => d.Type == DoorType.Trap);
    }

    public string Generate(GameState state, Random rng)
    {
        var treasureIndex = state.Doors.FindIndex(d => d.Type == DoorType.Treasure);
        var trapIndex = state.Doors.FindIndex(d => d.Type == DoorType.Trap);

        var relation = treasureIndex < trapIndex ? "before" : "after";

        return $"The Treasure comes {relation} the Trap in alphabetical door order.";
    }
}
