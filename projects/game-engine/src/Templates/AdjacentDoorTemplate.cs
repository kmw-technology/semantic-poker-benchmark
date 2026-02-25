using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The door immediately {left/right} of the {Treasure/Trap} is {Empty/not the Trap/not the Treasure}."
/// Checks the neighbor in the specified direction. Only applicable when the item is not at the boundary
/// in that direction.
/// </summary>
public class AdjacentDoorTemplate : ISentenceTemplate
{
    public string TemplateId => "adjacent-door";
    public string Description => "States what is (or is not) immediately adjacent to the Treasure or Trap.";

    public bool IsApplicable(GameState state)
    {
        // Applicable if at least one item (Treasure or Trap) has at least one neighbor.
        // Since doors A-E are 5 doors, every item has at least one neighbor unless there are fewer than 2 doors.
        return state.Doors.Count == 5;
    }

    public string Generate(GameState state, Random rng)
    {
        var items = new[] { DoorType.Treasure, DoorType.Trap };
        var chosenItem = items[rng.Next(items.Length)];
        var itemName = chosenItem == DoorType.Treasure ? "Treasure" : "Trap";
        var otherItemName = chosenItem == DoorType.Treasure ? "Trap" : "Treasure";

        var itemIndex = state.Doors.FindIndex(d => d.Type == chosenItem);

        // Determine valid directions
        var directions = new List<string>();
        if (itemIndex > 0) directions.Add("left");
        if (itemIndex < 4) directions.Add("right");

        var direction = directions[rng.Next(directions.Count)];
        var neighborIndex = direction == "left" ? itemIndex - 1 : itemIndex + 1;
        var neighbor = state.Doors[neighborIndex];

        // Generate a truthful description of the neighbor
        string description;
        if (neighbor.Type == DoorType.Empty)
        {
            // We can say "is Empty" or "is not the {OtherItem}" -- both are true.
            // Prefer the more informative "is Empty" sometimes, "is not the X" other times.
            if (rng.Next(2) == 0)
            {
                description = "Empty";
            }
            else
            {
                description = $"not the {otherItemName}";
            }
        }
        else if (neighbor.Type == chosenItem)
        {
            // This shouldn't happen (there's only one of each), but handle gracefully.
            description = $"not the {otherItemName}";
        }
        else
        {
            // Neighbor is the other special item. Say "not Empty" or identify truthfully.
            description = $"not the {itemName}";
        }

        return $"The door immediately {direction} of the {itemName} is {description}.";
    }
}
