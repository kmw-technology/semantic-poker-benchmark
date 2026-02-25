using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class EmptyNeighborTemplateTests
{
    private readonly EmptyNeighborTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("empty-neighbor", _template.TemplateId);
    }

    [Fact]
    public void IsApplicable_ValidFiveDoorState_ReturnsTrue()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        Assert.True(_template.IsApplicable(state));
    }

    [Fact]
    public void IsApplicable_NonFiveDoorState_ReturnsFalse()
    {
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Trap }
            }
        };
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_TreasureAtEdge_OneNeighbor_CorrectCount()
    {
        // Treasure at A(0): only right neighbor is B (Empty). Trap at C(2).
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 2);

        var foundTreasureCount = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            if (text.Contains("Treasure"))
            {
                // A's right neighbor is B (Empty). Only 1 neighbor (left boundary).
                Assert.Contains("exactly 1 Empty door", text);
                Assert.Contains("neighbor", text);
                foundTreasureCount = true;
            }
        }
        Assert.True(foundTreasureCount);
    }

    [Fact]
    public void Generate_ItemInMiddle_BothNeighborsEmpty_CountIsTwo()
    {
        // Treasure at C(2), Trap at A(0). B and D are Empty neighbors of C.
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 0);

        var foundTreasureWithTwo = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            if (text.Contains("Treasure"))
            {
                // C has neighbors B (Empty) and D (Empty) = 2
                Assert.Contains("exactly 2 Empty doors", text);
                Assert.Contains("neighbors", text);
                foundTreasureWithTwo = true;
            }
        }
        Assert.True(foundTreasureWithTwo);
    }

    [Fact]
    public void Generate_ItemAdjacentToOtherItem_CorrectCount()
    {
        // Treasure at B(1), Trap at C(2).
        // B's neighbors: A(Empty), C(Trap) -> 1 empty neighbor
        // C's neighbors: B(Treasure), D(Empty) -> 1 empty neighbor
        var state = TestHelpers.CreateState(treasureIndex: 1, trapIndex: 2);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // Both items have exactly 1 empty neighbor
            Assert.Contains("exactly 1 Empty door", text);
        }
    }

    [Fact]
    public void Generate_ItemSurroundedByNonEmpty_CountIsZero()
    {
        // Treasure at B(1), Trap at A(0).
        // A(Trap) has right neighbor B(Treasure), no left neighbor = 0 empty neighbors
        var state = TestHelpers.CreateState(treasureIndex: 1, trapIndex: 0);

        var foundTrapWithZero = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            if (text.Contains("Trap"))
            {
                // A(Trap) neighbors: none on left, B(Treasure) on right = 0 empty
                Assert.Contains("exactly 0 Empty doors", text);
                foundTrapWithZero = true;
            }
        }
        Assert.True(foundTrapWithZero);
    }

    [Fact]
    public void Generate_SingularVsPlural_CorrectGrammar()
    {
        // Treasure at A(0), Trap at E(4) -- both at edges
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // A's right neighbor is B (Empty) = 1
            // E's left neighbor is D (Empty) = 1
            // Both should be singular
            Assert.Contains("1 Empty door as immediate neighbor", text);
        }
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^The (Treasure|Trap) has exactly \d+ Empty doors? as immediate neighbors?\.$", text);
    }
}
