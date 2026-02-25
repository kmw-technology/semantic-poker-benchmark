using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The {Treasure/Trap} has exactly {N} Empty door(s) as immediate neighbor(s)."
/// Counts how many of the item's immediate neighbors (left, right) are Empty.
/// </summary>
public class EmptyNeighborTemplate : ISentenceTemplate
{
    public string TemplateId => "empty-neighbor";
    public string Description => "States the exact number of Empty doors immediately adjacent to the Treasure or Trap.";

    public bool IsApplicable(GameState state)
    {
        // Always applicable.
        return state.Doors.Count == 5;
    }

    public string Generate(GameState state, Random rng)
    {
        var items = new[] { DoorType.Treasure, DoorType.Trap };
        var chosenItem = items[rng.Next(items.Length)];
        var itemName = chosenItem == DoorType.Treasure ? "Treasure" : "Trap";

        var itemIndex = state.Doors.FindIndex(d => d.Type == chosenItem);

        var emptyNeighborCount = 0;
        if (itemIndex > 0 && state.Doors[itemIndex - 1].Type == DoorType.Empty)
            emptyNeighborCount++;
        if (itemIndex < 4 && state.Doors[itemIndex + 1].Type == DoorType.Empty)
            emptyNeighborCount++;

        var doorWord = emptyNeighborCount == 1 ? "Empty door" : "Empty doors";
        var neighborWord = emptyNeighborCount == 1 ? "neighbor" : "neighbors";

        return $"The {itemName} has exactly {emptyNeighborCount} {doorWord} as immediate {neighborWord}.";
    }
}
