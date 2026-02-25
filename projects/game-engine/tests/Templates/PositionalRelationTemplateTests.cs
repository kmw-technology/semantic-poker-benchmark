using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class PositionalRelationTemplateTests
{
    private readonly PositionalRelationTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("positional-relation", _template.TemplateId);
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

    [Theory]
    [InlineData(0, 1, 1)] // A-B = distance 1
    [InlineData(0, 4, 4)] // A-E = distance 4
    [InlineData(1, 3, 2)] // B-D = distance 2
    [InlineData(0, 3, 3)] // A-D = distance 3
    [InlineData(3, 1, 2)] // D-B = distance 2 (absolute)
    public void Generate_CorrectDistance(int treasureIdx, int trapIdx, int expectedDistance)
    {
        var state = TestHelpers.CreateState(treasureIndex: treasureIdx, trapIndex: trapIdx);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains($"exactly {expectedDistance}", text);
    }

    [Fact]
    public void Generate_Distance1_UsesSingularDoor()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 1);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("1 door away", text);
        Assert.DoesNotContain("doors away", text);
    }

    [Fact]
    public void Generate_DistanceGreaterThan1_UsesPluralDoors()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("4 doors away", text);
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^The Trap is exactly \d+ doors? away from the Treasure\.$", text);
    }
}
