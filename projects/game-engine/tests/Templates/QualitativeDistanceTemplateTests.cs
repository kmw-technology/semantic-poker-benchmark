using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class QualitativeDistanceTemplateTests
{
    private readonly QualitativeDistanceTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("qualitative-distance", _template.TemplateId);
    }

    [Fact]
    public void IsApplicable_ValidState_ReturnsTrue()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        Assert.True(_template.IsApplicable(state));
    }

    [Fact]
    public void IsApplicable_NoTreasure_ReturnsFalse()
    {
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Empty },
                new() { Label = 'B', Type = DoorType.Trap },
                new() { Label = 'C', Type = DoorType.Empty },
                new() { Label = 'D', Type = DoorType.Empty },
                new() { Label = 'E', Type = DoorType.Empty }
            }
        };
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_Distance1_SaysAdjacent()
    {
        // Treasure at A(0), Trap at B(1) -- distance 1
        var state = TestHelpers.CreateAdjacentState();
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Equal("The Treasure and the Trap are adjacent.", text);
    }

    [Fact]
    public void Generate_Distance2_SaysSeparatedByExactly1Door()
    {
        // Treasure at A(0), Trap at C(2) -- distance 2
        var state = TestHelpers.CreateDistance2State();
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Equal("The Treasure and the Trap are separated by exactly 1 door.", text);
    }

    [Fact]
    public void Generate_Distance3_SaysFarApartOrSeparated()
    {
        // Treasure at A(0), Trap at D(3) -- distance 3
        var state = TestHelpers.CreateDistance3State();

        var possibleOutputs = new HashSet<string>
        {
            "The Treasure and the Trap are far apart.",
            "The Treasure and the Trap are separated by at least 2 doors."
        };

        // Run with various seeds to see both possible outputs
        var seenOutputs = new HashSet<string>();
        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            Assert.Contains(text, possibleOutputs);
            seenOutputs.Add(text);
        }

        // With 100 seeds and 50/50 random chance, we should see both
        Assert.Equal(2, seenOutputs.Count);
    }

    [Fact]
    public void Generate_Distance4_SaysFarApartOrSeparated()
    {
        // Treasure at A(0), Trap at E(4) -- distance 4
        var state = TestHelpers.CreateFarApartState();

        var possibleOutputs = new HashSet<string>
        {
            "The Treasure and the Trap are far apart.",
            "The Treasure and the Trap are separated by at least 2 doors."
        };

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);
            Assert.Contains(text, possibleOutputs);
        }
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(0, 2, 2)]
    [InlineData(0, 3, 3)]
    [InlineData(0, 4, 4)]
    [InlineData(4, 3, 1)]
    [InlineData(4, 0, 4)]
    public void Generate_OutputIsTruthfulForDistance(int treasureIdx, int trapIdx, int expectedDistance)
    {
        var state = TestHelpers.CreateState(treasureIndex: treasureIdx, trapIndex: trapIdx);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        if (expectedDistance == 1)
        {
            Assert.Contains("adjacent", text);
        }
        else if (expectedDistance == 2)
        {
            Assert.Contains("separated by exactly 1 door", text);
        }
        else
        {
            // distance >= 3: "far apart" or "separated by at least 2 doors"
            Assert.True(
                text.Contains("far apart") || text.Contains("separated by at least 2 doors"),
                $"Unexpected output for distance {expectedDistance}: {text}");
        }
    }
}
