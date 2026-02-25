using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class PairExclusionTemplateTests
{
    private readonly PairExclusionTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("pair-exclusion", _template.TemplateId);
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
                new() { Label = 'B', Type = DoorType.Trap },
                new() { Label = 'C', Type = DoorType.Empty }
            }
        };
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_OutputIsTruthful()
    {
        // Treasure at B(1), Trap at D(3)
        var state = TestHelpers.CreateState(treasureIndex: 1, trapIndex: 3);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // Pattern: "The {Treasure/Trap} is not behind Door {X} or Door {Y}."
            Assert.Matches(@"^The (Treasure|Trap) is not behind Door [A-E] or Door [A-E]\.$", text);

            // Extract item and doors
            var isTreasure = text.Contains("Treasure is not");
            var doorChars = new List<char>();
            var parts = text.Split("Door ");
            for (int i = 1; i < parts.Length; i++)
            {
                doorChars.Add(parts[i][0]);
            }

            // Verify the mentioned doors do NOT contain the item
            foreach (var doorLabel in doorChars)
            {
                var door = state.Doors.First(d => d.Label == doorLabel);
                if (isTreasure)
                {
                    Assert.NotEqual(DoorType.Treasure, door.Type);
                }
                else
                {
                    Assert.NotEqual(DoorType.Trap, door.Type);
                }
            }
        }
    }

    [Fact]
    public void Generate_MentionsTwoDifferentDoors()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        // Extract the two door labels
        var parts = text.Split("Door ");
        // parts[0] = "The Treasure/Trap is not behind "
        // parts[1] = "X or "
        // parts[2] = "Y."
        var door1 = parts[1][0];
        var door2 = parts[2][0];

        Assert.NotEqual(door1, door2);
    }

    [Fact]
    public void Generate_DoorsAreInAlphabeticalOrder()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);

        for (int seed = 0; seed < 30; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            var parts = text.Split("Door ");
            var door1 = parts[1][0];
            var door2 = parts[2][0];

            Assert.True(door1 < door2, $"Expected doors in alphabetical order but got {door1} and {door2}");
        }
    }
}
