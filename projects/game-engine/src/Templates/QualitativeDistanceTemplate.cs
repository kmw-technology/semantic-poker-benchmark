using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Templates;

/// <summary>
/// Generates: "The Treasure and the Trap are {adjacent/far apart/separated by at least 2 doors}."
/// Adjacent = distance 1, separated by at least 2 = distance >= 3, far apart = distance >= 3.
/// </summary>
public class QualitativeDistanceTemplate : ISentenceTemplate
{
    public string TemplateId => "qualitative-distance";
    public string Description => "Describes the qualitative distance between the Treasure and the Trap.";

    public bool IsApplicable(GameState state)
    {
        // Always applicable when both exist.
        return state.Doors.Count == 5
               && state.Doors.Any(d => d.Type == DoorType.Treasure)
               && state.Doors.Any(d => d.Type == DoorType.Trap);
    }

    public string Generate(GameState state, Random rng)
    {
        var treasureIndex = state.Doors.FindIndex(d => d.Type == DoorType.Treasure);
        var trapIndex = state.Doors.FindIndex(d => d.Type == DoorType.Trap);
        var distance = Math.Abs(treasureIndex - trapIndex);

        if (distance == 1)
        {
            return "The Treasure and the Trap are adjacent.";
        }
        else if (distance == 2)
        {
            return "The Treasure and the Trap are separated by exactly 1 door.";
        }
        else if (distance >= 3)
        {
            // distance can be 3 or 4; both qualify for "separated by at least 2 doors" and "far apart"
            if (rng.Next(2) == 0)
            {
                return "The Treasure and the Trap are far apart.";
            }
            else
            {
                return "The Treasure and the Trap are separated by at least 2 doors.";
            }
        }

        // Fallback (should not reach here with valid state)
        return "The Treasure and the Trap are separated by at least 2 doors.";
    }
}
