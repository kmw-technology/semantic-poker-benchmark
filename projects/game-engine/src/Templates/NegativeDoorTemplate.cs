using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The {Treasure/Trap} is not behind Door {X}."
/// Picks a door that does NOT contain the specified item.
/// </summary>
public class NegativeDoorTemplate : ISentenceTemplate
{
    public string TemplateId => "negative-door";
    public string Description => "States that a specific item is not behind a specific door.";

    public bool IsApplicable(GameState state)
    {
        // Always applicable: there are always at least 4 doors that don't have a given item.
        return state.Doors.Count == 5;
    }

    public string Generate(GameState state, Random rng)
    {
        // Pick Treasure or Trap randomly
        var items = new[] { DoorType.Treasure, DoorType.Trap };
        var chosenItem = items[rng.Next(items.Length)];
        var itemName = chosenItem == DoorType.Treasure ? "Treasure" : "Trap";

        // Get doors that do NOT have this item
        var candidateDoors = state.Doors
            .Where(d => d.Type != chosenItem)
            .ToList();

        var pickedDoor = candidateDoors[rng.Next(candidateDoors.Count)];

        return $"The {itemName} is not behind Door {pickedDoor.Label}.";
    }
}
