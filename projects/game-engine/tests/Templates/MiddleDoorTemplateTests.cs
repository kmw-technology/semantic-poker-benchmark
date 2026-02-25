using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class MiddleDoorTemplateTests
{
    private readonly MiddleDoorTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("middle-door", _template.TemplateId);
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
    public void Generate_TreasureAtC_SaysContainsTreasure()
    {
        // Treasure at C(2)
        var state = TestHelpers.CreateTreasureAtCState();

        var foundContains = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);
            if (text == "Door C contains the Treasure.")
            {
                foundContains = true;
                break;
            }
        }
        Assert.True(foundContains);
    }

    [Fact]
    public void Generate_TrapAtC_SaysContainsTrap()
    {
        // Trap at C(2)
        var state = TestHelpers.CreateTrapAtCState();

        var foundContains = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);
            if (text == "Door C contains the Trap.")
            {
                foundContains = true;
                break;
            }
        }
        Assert.True(foundContains);
    }

    [Fact]
    public void Generate_NeitherAtC_SaysDoesNotContain()
    {
        // Treasure at A(0), Trap at E(4) -- C is Empty
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // C is Empty, so it should always say "does not contain"
            Assert.Contains("does not contain", text);
        }
    }

    [Fact]
    public void Generate_OutputIsTruthful()
    {
        // Treasure at C(2), Trap at A(0)
        var state = TestHelpers.CreateTreasureAtCState();

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            var doorC = state.Doors.First(d => d.Label == 'C');

            if (text.Contains("contains the Treasure"))
            {
                Assert.Equal(DoorType.Treasure, doorC.Type);
            }
            else if (text.Contains("does not contain the Treasure"))
            {
                Assert.NotEqual(DoorType.Treasure, doorC.Type);
            }
            else if (text.Contains("contains the Trap"))
            {
                Assert.Equal(DoorType.Trap, doorC.Type);
            }
            else if (text.Contains("does not contain the Trap"))
            {
                Assert.NotEqual(DoorType.Trap, doorC.Type);
            }
        }
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^Door C (contains|does not contain) the (Treasure|Trap)\.$", text);
    }
}
