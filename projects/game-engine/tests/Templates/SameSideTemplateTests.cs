using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class SameSideTemplateTests
{
    private readonly SameSideTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("same-side", _template.TemplateId);
    }

    [Fact]
    public void IsApplicable_BothOnLeftSide_ReturnsTrue()
    {
        // Treasure=A(0), Trap=B(1) -- both in left half
        var state = TestHelpers.CreateBothLeftState();
        Assert.True(_template.IsApplicable(state));
    }

    [Fact]
    public void IsApplicable_BothOnRightSide_ReturnsTrue()
    {
        // Treasure=D(3), Trap=E(4) -- both in right half
        var state = TestHelpers.CreateBothRightState();
        Assert.True(_template.IsApplicable(state));
    }

    [Fact]
    public void IsApplicable_OneOnEachSide_ReturnsFalse()
    {
        // Treasure=A(0), Trap=E(4) -- opposite sides
        var state = TestHelpers.CreateFarApartState();
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void IsApplicable_OneInMiddle_ReturnsFalse()
    {
        // Treasure=C(2), Trap=D(3) -- C is middle (neither side)
        var state = TestHelpers.CreateMiddleState();
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void IsApplicable_TreasureAtMiddleTrapOnSide_ReturnsFalse()
    {
        // Treasure=C(2), Trap=A(0) -- C is middle
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 0);
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_BothLeft_SaysLeftHalf()
    {
        var state = TestHelpers.CreateBothLeftState();
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("left half", text);
    }

    [Fact]
    public void Generate_BothRight_SaysRightHalf()
    {
        var state = TestHelpers.CreateBothRightState();
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("right half", text);
    }

    [Fact]
    public void Generate_MentionsBothItems()
    {
        var state = TestHelpers.CreateBothLeftState();
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Contains("Treasure", text);
        Assert.Contains("Trap", text);
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateBothLeftState();
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^The (Treasure|Trap) and the (Treasure|Trap) are both in the (left|right) half\.$", text);
    }
}
