using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class OrderTemplateTests
{
    private readonly OrderTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("order", _template.TemplateId);
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
    public void IsApplicable_NoTrap_ReturnsFalse()
    {
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Empty },
                new() { Label = 'C', Type = DoorType.Empty },
                new() { Label = 'D', Type = DoorType.Empty },
                new() { Label = 'E', Type = DoorType.Empty }
            }
        };
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_TreasureBeforeTrap_SaysBefore()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Equal("The Treasure comes before the Trap in alphabetical door order.", text);
    }

    [Fact]
    public void Generate_TreasureAfterTrap_SaysAfter()
    {
        // Treasure at E(4), Trap at A(0)
        var state = TestHelpers.CreateState(treasureIndex: 4, trapIndex: 0);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Equal("The Treasure comes after the Trap in alphabetical door order.", text);
    }

    [Theory]
    [InlineData(0, 1, "before")]
    [InlineData(0, 4, "before")]
    [InlineData(1, 0, "after")]
    [InlineData(4, 0, "after")]
    [InlineData(2, 3, "before")]
    [InlineData(3, 2, "after")]
    public void Generate_CorrectRelation(int treasureIdx, int trapIdx, string expectedRelation)
    {
        var state = TestHelpers.CreateState(treasureIndex: treasureIdx, trapIndex: trapIdx);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains(expectedRelation, text);
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^The Treasure comes (before|after) the Trap in alphabetical door order\.$", text);
    }
}
