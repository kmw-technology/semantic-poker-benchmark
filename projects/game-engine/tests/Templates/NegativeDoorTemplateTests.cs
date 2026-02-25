using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class NegativeDoorTemplateTests
{
    private readonly NegativeDoorTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("negative-door", _template.TemplateId);
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
    public void Generate_OutputIsTruthful_TreasureNotBehindDoor()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);

        // Generate multiple times with different seeds to cover both Treasure and Trap branches
        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // Parse the output: "The {Treasure/Trap} is not behind Door {X}."
            Assert.Matches(@"^The (Treasure|Trap) is not behind Door [A-E]\.$", text);

            // Verify truthfulness
            if (text.Contains("Treasure"))
            {
                // The mentioned door should NOT be the Treasure door
                var doorLabel = text[^2]; // second-to-last char is the door label
                Assert.NotEqual('A', doorLabel); // A is the Treasure
            }
            else if (text.Contains("Trap"))
            {
                var doorLabel = text[^2];
                Assert.NotEqual('E', doorLabel); // E is the Trap
            }
        }
    }

    [Fact]
    public void Generate_NeverClaimsItemIsNotBehindItsActualDoor()
    {
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 3); // C=Treasure, D=Trap

        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            if (text.StartsWith("The Treasure"))
            {
                // Should not mention Door C (where Treasure actually is)
                Assert.DoesNotContain("Door C", text);
            }
            else
            {
                // Should not mention Door D (where Trap actually is)
                Assert.DoesNotContain("Door D", text);
            }
        }
    }
}
