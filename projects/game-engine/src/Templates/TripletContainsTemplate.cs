using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The {Treasure/Trap} is behind Door {X}, Door {Y}, or Door {Z}."
/// Includes the actual door containing the item plus 2 randomly chosen other doors.
/// </summary>
public class TripletContainsTemplate : ISentenceTemplate
{
    public string TemplateId => "triplet-contains";
    public string Description => "States that a specific item is behind one of three named doors (one of which is correct).";

    public bool IsApplicable(GameState state)
    {
        // Always applicable: the item is behind 1 door, and there are 4 others to pick 2 from.
        return state.Doors.Count == 5;
    }

    public string Generate(GameState state, Random rng)
    {
        var items = new[] { DoorType.Treasure, DoorType.Trap };
        var chosenItem = items[rng.Next(items.Length)];
        var itemName = chosenItem == DoorType.Treasure ? "Treasure" : "Trap";

        var actualDoor = state.Doors.First(d => d.Type == chosenItem);

        // Pick 2 other doors randomly
        var otherDoors = state.Doors
            .Where(d => d.Label != actualDoor.Label)
            .OrderBy(_ => rng.Next())
            .Take(2)
            .ToList();

        // Combine and sort by label for consistent output
        var triplet = new List<Door> { actualDoor };
        triplet.AddRange(otherDoors);
        triplet = triplet.OrderBy(d => d.Label).ToList();

        return $"The {itemName} is behind Door {triplet[0].Label}, Door {triplet[1].Label}, or Door {triplet[2].Label}.";
    }
}
