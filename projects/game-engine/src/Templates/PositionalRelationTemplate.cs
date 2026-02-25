using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The Trap is exactly {N} door(s) away from the Treasure."
/// N = |index(Trap) - index(Treasure)|.
/// </summary>
public class PositionalRelationTemplate : ISentenceTemplate
{
    public string TemplateId => "positional-relation";
    public string Description => "States the exact positional distance between the Treasure and the Trap.";

    public bool IsApplicable(GameState state)
    {
        // Always applicable when both Treasure and Trap exist.
        return state.Doors.Count == 5
               && state.Doors.Any(d => d.Type == DoorType.Treasure)
               && state.Doors.Any(d => d.Type == DoorType.Trap);
    }

    public string Generate(GameState state, Random rng)
    {
        var treasureIndex = state.Doors.FindIndex(d => d.Type == DoorType.Treasure);
        var trapIndex = state.Doors.FindIndex(d => d.Type == DoorType.Trap);
        var distance = Math.Abs(trapIndex - treasureIndex);

        var doorWord = distance == 1 ? "door" : "doors";

        return $"The Trap is exactly {distance} {doorWord} away from the Treasure.";
    }
}
