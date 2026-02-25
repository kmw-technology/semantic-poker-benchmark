using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "There {is/are} exactly {N} Empty door(s) between the Treasure and the Trap."
/// Counts only Empty doors strictly between the two items' positions.
/// </summary>
public class GapTemplate : ISentenceTemplate
{
    public string TemplateId => "gap";
    public string Description => "States the exact number of Empty doors between the Treasure and the Trap.";

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

        var lower = Math.Min(treasureIndex, trapIndex);
        var upper = Math.Max(treasureIndex, trapIndex);

        var emptyBetween = 0;
        for (int i = lower + 1; i < upper; i++)
        {
            if (state.Doors[i].Type == DoorType.Empty)
                emptyBetween++;
        }

        var verb = emptyBetween == 1 ? "is" : "are";
        var doorWord = emptyBetween == 1 ? "Empty door" : "Empty doors";

        return $"There {verb} exactly {emptyBetween} {doorWord} between the Treasure and the Trap.";
    }
}
