using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class EndpointTemplateTests
{
    private readonly EndpointTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("endpoint", _template.TemplateId);
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
            Doors = new List<Door> { new() { Label = 'A', Type = DoorType.Treasure } }
        };
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_TreasureAtEndA_SaysIsAtEnd()
    {
        // Treasure at A(0)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 2);

        // Run with many seeds; when it picks Treasure, should say "is at either end"
        var foundTreasureAtEnd = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);
            if (text.Contains("Treasure") && text.Contains("is at either end"))
            {
                foundTreasureAtEnd = true;
                break;
            }
        }
        Assert.True(foundTreasureAtEnd);
    }

    [Fact]
    public void Generate_TrapAtEndE_SaysIsAtEnd()
    {
        // Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 4);

        var foundTrapAtEnd = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);
            if (text.Contains("Trap") && text.Contains("is at either end"))
            {
                foundTrapAtEnd = true;
                break;
            }
        }
        Assert.True(foundTrapAtEnd);
    }

    [Fact]
    public void Generate_ItemInMiddle_SaysIsNotAtEnd()
    {
        // Treasure at C(2), Trap at B(1) -- both NOT at endpoints
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 1);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // Both items are in the middle, so always "is not at either end"
            Assert.Contains("is not at either end", text);
        }
    }

    [Fact]
    public void Generate_TruthfulForAllPositions()
    {
        // Test all possible Treasure positions
        for (int treasureIdx = 0; treasureIdx < 5; treasureIdx++)
        {
            var trapIdx = treasureIdx == 0 ? 2 : 0;
            var state = TestHelpers.CreateState(treasureIndex: treasureIdx, trapIndex: trapIdx);
            var isAtEnd = treasureIdx == 0 || treasureIdx == 4;

            for (int seed = 0; seed < 20; seed++)
            {
                var rng = new Random(seed);
                var text = _template.Generate(state, rng);

                if (text.Contains("Treasure"))
                {
                    if (isAtEnd)
                        Assert.Contains("is at either end", text);
                    else
                        Assert.Contains("is not at either end", text);
                }
            }
        }
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^The (Treasure|Trap) is (not )?at either end of the row\.$", text);
    }
}
