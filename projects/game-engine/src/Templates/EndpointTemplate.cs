using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The {Treasure/Trap} is {/not} at either end of the row."
/// Ends are Door A (index 0) and Door E (index 4).
/// </summary>
public class EndpointTemplate : ISentenceTemplate
{
    public string TemplateId => "endpoint";
    public string Description => "States whether the Treasure or Trap is at either end of the row.";

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
        var isAtEnd = itemIndex == 0 || itemIndex == 4;

        if (isAtEnd)
        {
            return $"The {itemName} is at either end of the row.";
        }
        else
        {
            return $"The {itemName} is not at either end of the row.";
        }
    }
}
