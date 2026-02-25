using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine;

public class StateValidator : IStateValidator
{
    private static readonly char[] ExpectedLabels = { 'A', 'B', 'C', 'D', 'E' };

    public bool IsValidGameState(GameState state)
    {
        return GetValidationErrors(state).Count == 0;
    }

    public List<string> GetValidationErrors(GameState state)
    {
        var errors = new List<string>();

        if (state.Doors == null)
        {
            errors.Add("Doors collection is null.");
            return errors;
        }

        if (state.Doors.Count != 5)
        {
            errors.Add($"Expected exactly 5 doors, but found {state.Doors.Count}.");
        }

        var labels = state.Doors.Select(d => d.Label).OrderBy(l => l).ToArray();
        if (!labels.SequenceEqual(ExpectedLabels))
        {
            errors.Add($"Door labels must be exactly A, B, C, D, E (unique). Found: {string.Join(", ", labels)}.");
        }

        var treasureCount = state.Doors.Count(d => d.Type == DoorType.Treasure);
        if (treasureCount != 1)
        {
            errors.Add($"Expected exactly 1 Treasure door, but found {treasureCount}.");
        }

        var trapCount = state.Doors.Count(d => d.Type == DoorType.Trap);
        if (trapCount != 1)
        {
            errors.Add($"Expected exactly 1 Trap door, but found {trapCount}.");
        }

        var emptyCount = state.Doors.Count(d => d.Type == DoorType.Empty);
        if (emptyCount != 3)
        {
            errors.Add($"Expected exactly 3 Empty doors, but found {emptyCount}.");
        }

        return errors;
    }
}
