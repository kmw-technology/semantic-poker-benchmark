using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The {item1} and the {item2} are both in the {left/right} half."
/// Left half = {A, B}, Right half = {D, E}. Door C is neither.
/// Only applicable if both items are on the same side (both left or both right).
/// </summary>
public class SameSideTemplate : ISentenceTemplate
{
    public string TemplateId => "same-side";
    public string Description => "States that two items are both in the same half of the row.";

    public bool IsApplicable(GameState state)
    {
        var treasureIndex = state.Doors.FindIndex(d => d.Type == DoorType.Treasure);
        var trapIndex = state.Doors.FindIndex(d => d.Type == DoorType.Trap);

        // Both on the left side (indices 0, 1 = doors A, B)
        if (treasureIndex <= 1 && trapIndex <= 1) return true;

        // Both on the right side (indices 3, 4 = doors D, E)
        if (treasureIndex >= 3 && trapIndex >= 3) return true;

        return false;
    }

    public string Generate(GameState state, Random rng)
    {
        var treasureIndex = state.Doors.FindIndex(d => d.Type == DoorType.Treasure);
        var trapIndex = state.Doors.FindIndex(d => d.Type == DoorType.Trap);

        var side = (treasureIndex <= 1 && trapIndex <= 1) ? "left" : "right";

        // Randomly choose the order of items in the sentence
        string item1, item2;
        if (rng.Next(2) == 0)
        {
            item1 = "Treasure";
            item2 = "Trap";
        }
        else
        {
            item1 = "Trap";
            item2 = "Treasure";
        }

        return $"The {item1} and the {item2} are both in the {side} half.";
    }
}
