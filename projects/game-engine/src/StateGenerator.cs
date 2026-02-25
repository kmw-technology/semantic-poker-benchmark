using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Interfaces;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine;

public class StateGenerator : IStateGenerator
{
    private static readonly char[] DoorLabels = { 'A', 'B', 'C', 'D', 'E' };

    public GameState Generate(int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var doors = DoorLabels
            .Select(label => new Door { Label = label, Type = DoorType.Empty })
            .ToList();

        var indices = Enumerable.Range(0, 5).OrderBy(_ => rng.Next()).ToList();
        doors[indices[0]].Type = DoorType.Treasure;
        doors[indices[1]].Type = DoorType.Trap;

        return new GameState
        {
            Id = Guid.NewGuid(),
            Doors = doors,
            Seed = seed ?? rng.Next(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
