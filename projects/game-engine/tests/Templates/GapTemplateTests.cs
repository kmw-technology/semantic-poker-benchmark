using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class GapTemplateTests
{
    private readonly GapTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("gap", _template.TemplateId);
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

    [Theory]
    [InlineData(0, 1, 0)] // Adjacent: 0 empty between
    [InlineData(0, 2, 1)] // 1 empty between (B is empty)
    [InlineData(0, 4, 3)] // 3 empty between (B, C, D are all empty)
    [InlineData(1, 3, 1)] // 1 empty between (C is empty)
    [InlineData(0, 3, 2)] // 2 empty between (B, C are empty)
    public void Generate_CorrectEmptyCount(int treasureIdx, int trapIdx, int expectedEmpty)
    {
        var state = TestHelpers.CreateState(treasureIndex: treasureIdx, trapIndex: trapIdx);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains($"exactly {expectedEmpty}", text);
    }

    [Fact]
    public void Generate_OneEmpty_UsesSingular()
    {
        // Treasure at A(0), Trap at C(2): B is empty between them
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 2);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("1 Empty door between", text);
        Assert.Contains("There is", text);
    }

    [Fact]
    public void Generate_MultipleEmpty_UsesPlural()
    {
        // Treasure at A(0), Trap at E(4): B, C, D are empty between them
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("3 Empty doors between", text);
        Assert.Contains("There are", text);
    }

    [Fact]
    public void Generate_ZeroEmpty_UsesPlural()
    {
        // Treasure at A(0), Trap at B(1): adjacent, 0 empty between
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 1);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("0 Empty doors", text);
        Assert.Contains("There are", text);
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^There (is|are) exactly \d+ Empty doors? between the Treasure and the Trap\.$", text);
    }
}
