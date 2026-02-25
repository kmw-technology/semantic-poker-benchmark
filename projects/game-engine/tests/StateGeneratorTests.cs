using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests;

public class StateGeneratorTests
{
    private readonly StateGenerator _generator = new();

    [Fact]
    public void Generate_CreatesStateWithFiveDoors()
    {
        var state = _generator.Generate(seed: 123);

        Assert.NotNull(state);
        Assert.NotNull(state.Doors);
        Assert.Equal(5, state.Doors.Count);
    }

    [Fact]
    public void Generate_HasExactlyOneTreasureOneTrapThreeEmpty()
    {
        var state = _generator.Generate(seed: 123);

        var treasureCount = state.Doors.Count(d => d.Type == DoorType.Treasure);
        var trapCount = state.Doors.Count(d => d.Type == DoorType.Trap);
        var emptyCount = state.Doors.Count(d => d.Type == DoorType.Empty);

        Assert.Equal(1, treasureCount);
        Assert.Equal(1, trapCount);
        Assert.Equal(3, emptyCount);
    }

    [Fact]
    public void Generate_DoorsAreLabeledAToE()
    {
        var state = _generator.Generate(seed: 123);

        var labels = state.Doors.Select(d => d.Label).OrderBy(l => l).ToArray();
        Assert.Equal(new[] { 'A', 'B', 'C', 'D', 'E' }, labels);
    }

    [Fact]
    public void Generate_DoorsAreLabeledInOrder()
    {
        var state = _generator.Generate(seed: 123);

        // Labels should be assigned in order A, B, C, D, E regardless of door types
        Assert.Equal('A', state.Doors[0].Label);
        Assert.Equal('B', state.Doors[1].Label);
        Assert.Equal('C', state.Doors[2].Label);
        Assert.Equal('D', state.Doors[3].Label);
        Assert.Equal('E', state.Doors[4].Label);
    }

    [Fact]
    public void Generate_SameSeedProducesSameState()
    {
        var state1 = _generator.Generate(seed: 42);
        var state2 = _generator.Generate(seed: 42);

        // Same seed should produce same door type assignments
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(state1.Doors[i].Label, state2.Doors[i].Label);
            Assert.Equal(state1.Doors[i].Type, state2.Doors[i].Type);
        }
    }

    [Fact]
    public void Generate_DifferentSeedsProduceDifferentStates()
    {
        // Try multiple seed pairs to find at least one that produces different layouts.
        // With 20 possible layouts (5 choose 2 for Treasure+Trap placement),
        // two random seeds are very likely to differ.
        var foundDifference = false;

        for (int seed = 0; seed < 50; seed++)
        {
            var state1 = _generator.Generate(seed: seed);
            var state2 = _generator.Generate(seed: seed + 1000);

            var types1 = state1.Doors.Select(d => d.Type).ToList();
            var types2 = state2.Doors.Select(d => d.Type).ToList();

            if (!types1.SequenceEqual(types2))
            {
                foundDifference = true;
                break;
            }
        }

        Assert.True(foundDifference, "Expected different seeds to eventually produce different states.");
    }

    [Fact]
    public void Generate_WithoutSeed_StillProducesValidState()
    {
        var state = _generator.Generate();

        Assert.Equal(5, state.Doors.Count);
        Assert.Equal(1, state.Doors.Count(d => d.Type == DoorType.Treasure));
        Assert.Equal(1, state.Doors.Count(d => d.Type == DoorType.Trap));
        Assert.Equal(3, state.Doors.Count(d => d.Type == DoorType.Empty));
    }

    [Fact]
    public void Generate_SetsStateId()
    {
        var state = _generator.Generate(seed: 123);

        Assert.NotEqual(Guid.Empty, state.Id);
    }

    [Fact]
    public void Generate_SetsSeedOnState()
    {
        var state = _generator.Generate(seed: 999);

        Assert.Equal(999, state.Seed);
    }

    [Fact]
    public void Generate_SetsCreatedAt()
    {
        var before = DateTime.UtcNow;
        var state = _generator.Generate(seed: 123);
        var after = DateTime.UtcNow;

        Assert.InRange(state.CreatedAt, before, after);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Generate_VariousSeeds_AlwaysProducesValidState(int seed)
    {
        var state = _generator.Generate(seed: seed);

        Assert.Equal(5, state.Doors.Count);
        Assert.Equal(1, state.Doors.Count(d => d.Type == DoorType.Treasure));
        Assert.Equal(1, state.Doors.Count(d => d.Type == DoorType.Trap));
        Assert.Equal(3, state.Doors.Count(d => d.Type == DoorType.Empty));

        var labels = state.Doors.Select(d => d.Label).OrderBy(l => l).ToArray();
        Assert.Equal(new[] { 'A', 'B', 'C', 'D', 'E' }, labels);
    }
}
