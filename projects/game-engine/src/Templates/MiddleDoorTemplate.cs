using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "Door C {contains/does not contain} the {Treasure/Trap}."
/// </summary>
public class MiddleDoorTemplate : ISentenceTemplate
{
    public string TemplateId => "middle-door";
    public string Description => "States whether Door C contains or does not contain the Treasure or Trap.";

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

        var doorC = state.Doors.First(d => d.Label == 'C');
        var containsItem = doorC.Type == chosenItem;

        if (containsItem)
        {
            return $"Door C contains the {itemName}.";
        }
        else
        {
            return $"Door C does not contain the {itemName}.";
        }
    }
}
