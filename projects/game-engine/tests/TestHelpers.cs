using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests;

/// <summary>
/// Helper methods for building test GameState instances with known configurations.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a GameState with the specified Treasure and Trap positions (0-based index).
    /// Remaining doors are Empty. Labels are always A-E.
    /// </summary>
    public static GameState CreateState(int treasureIndex, int trapIndex, int seed = 42)
    {
        if (treasureIndex == trapIndex)
            throw new ArgumentException("Treasure and Trap cannot be on the same door.");

        var labels = new[] { 'A', 'B', 'C', 'D', 'E' };
        var doors = labels.Select((label, i) => new Door
        {
            Label = label,
            Type = i == treasureIndex ? DoorType.Treasure
                 : i == trapIndex ? DoorType.Trap
                 : DoorType.Empty
        }).ToList();

        return new GameState
        {
            Id = Guid.NewGuid(),
            Doors = doors,
            Seed = seed,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a GameState where Treasure=A(0), Trap=E(4), Empty=B,C,D.
    /// This is the maximum distance configuration (distance=4).
    /// </summary>
    public static GameState CreateFarApartState(int seed = 42)
        => CreateState(treasureIndex: 0, trapIndex: 4, seed: seed);

    /// <summary>
    /// Creates a GameState where Treasure=A(0), Trap=B(1), Empty=C,D,E.
    /// Adjacent configuration with Treasure before Trap.
    /// </summary>
    public static GameState CreateAdjacentState(int seed = 42)
        => CreateState(treasureIndex: 0, trapIndex: 1, seed: seed);

    /// <summary>
    /// Creates a GameState where Treasure=C(2), Trap=D(3), Empty=A,B,E.
    /// Middle configuration.
    /// </summary>
    public static GameState CreateMiddleState(int seed = 42)
        => CreateState(treasureIndex: 2, trapIndex: 3, seed: seed);

    /// <summary>
    /// Creates a GameState where both Treasure and Trap are in the left half (A,B).
    /// Treasure=A(0), Trap=B(1).
    /// </summary>
    public static GameState CreateBothLeftState(int seed = 42)
        => CreateState(treasureIndex: 0, trapIndex: 1, seed: seed);

    /// <summary>
    /// Creates a GameState where both Treasure and Trap are in the right half (D,E).
    /// Treasure=D(3), Trap=E(4).
    /// </summary>
    public static GameState CreateBothRightState(int seed = 42)
        => CreateState(treasureIndex: 3, trapIndex: 4, seed: seed);

    /// <summary>
    /// Creates a GameState where Treasure is at Door C (middle).
    /// Treasure=C(2), Trap=A(0).
    /// </summary>
    public static GameState CreateTreasureAtCState(int seed = 42)
        => CreateState(treasureIndex: 2, trapIndex: 0, seed: seed);

    /// <summary>
    /// Creates a GameState where Trap is at Door C (middle).
    /// Treasure=A(0), Trap=C(2).
    /// </summary>
    public static GameState CreateTrapAtCState(int seed = 42)
        => CreateState(treasureIndex: 0, trapIndex: 2, seed: seed);

    /// <summary>
    /// Creates a GameState where Treasure is at endpoint A and Trap is at C.
    /// Treasure at endpoint, Trap not at endpoint.
    /// </summary>
    public static GameState CreateTreasureAtEndpointState(int seed = 42)
        => CreateState(treasureIndex: 0, trapIndex: 2, seed: seed);

    /// <summary>
    /// Creates a GameState where Trap is at endpoint E and Treasure is at C.
    /// </summary>
    public static GameState CreateTrapAtEndpointState(int seed = 42)
        => CreateState(treasureIndex: 2, trapIndex: 4, seed: seed);

    /// <summary>
    /// Creates a GameState with distance 2 between Treasure and Trap.
    /// Treasure=A(0), Trap=C(2), so distance=2, 1 door between them (B, which is Empty).
    /// </summary>
    public static GameState CreateDistance2State(int seed = 42)
        => CreateState(treasureIndex: 0, trapIndex: 2, seed: seed);

    /// <summary>
    /// Creates a GameState with distance 3 between Treasure and Trap.
    /// Treasure=A(0), Trap=D(3).
    /// </summary>
    public static GameState CreateDistance3State(int seed = 42)
        => CreateState(treasureIndex: 0, trapIndex: 3, seed: seed);

    /// <summary>
    /// Creates a PlayerDecision for a given model picking a given door.
    /// </summary>
    public static PlayerDecision CreateDecision(string modelId, char chosenDoor)
    {
        return new PlayerDecision
        {
            ModelId = modelId,
            Role = PlayerRole.Player,
            ChosenDoor = chosenDoor
        };
    }
}
