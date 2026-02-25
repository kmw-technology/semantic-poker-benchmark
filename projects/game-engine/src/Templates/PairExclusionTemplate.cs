using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The {Treasure/Trap} is not behind Door {X} or Door {Y}."
/// Picks 2 doors that do NOT contain the specified item.
/// </summary>
public class PairExclusionTemplate : ISentenceTemplate
{
    public string TemplateId => "pair-exclusion";
    public string Description => "States that a specific item is not behind either of two specific doors.";

    public bool IsApplicable(GameState state)
    {
        // Always applicable: each item occupies 1 door, so there are always 4 non-item doors
        // and we need to pick 2 of them.
        return state.Doors.Count == 5;
    }

    public string Generate(GameState state, Random rng)
    {
        var items = new[] { DoorType.Treasure, DoorType.Trap };
        var chosenItem = items[rng.Next(items.Length)];
        var itemName = chosenItem == DoorType.Treasure ? "Treasure" : "Trap";

        var candidateDoors = state.Doors
            .Where(d => d.Type != chosenItem)
            .OrderBy(_ => rng.Next())
            .Take(2)
            .OrderBy(d => d.Label)
            .ToList();

        return $"The {itemName} is not behind Door {candidateDoors[0].Label} or Door {candidateDoors[1].Label}.";
    }
}
