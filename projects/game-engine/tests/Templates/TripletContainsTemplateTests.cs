using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class TripletContainsTemplateTests
{
    private readonly TripletContainsTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("triplet-contains", _template.TemplateId);
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
    public void Generate_OutputContainsActualDoor()
    {
        // Treasure at C(2), Trap at A(0)
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 0);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // Pattern: "The {item} is behind Door X, Door Y, or Door Z."
            Assert.Matches(@"^The (Treasure|Trap) is behind Door [A-E], Door [A-E], or Door [A-E]\.$", text);

            // The actual door should always be in the triplet
            if (text.StartsWith("The Treasure"))
            {
                Assert.Contains("Door C", text); // Treasure is at C
            }
            else
            {
                Assert.Contains("Door A", text); // Trap is at A
            }
        }
    }

    [Fact]
    public void Generate_MentionsExactlyThreeDoors()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        var doorMentions = text.Split("Door ").Length - 1;
        Assert.Equal(3, doorMentions);
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
            var doors = new List<char>();
            for (int i = 1; i < parts.Length; i++)
            {
                doors.Add(parts[i][0]);
            }

            for (int i = 0; i < doors.Count - 1; i++)
            {
                Assert.True(doors[i] < doors[i + 1],
                    $"Expected doors in order but got {string.Join(", ", doors)}");
            }
        }
    }

    [Fact]
    public void Generate_AllMentionedDoorsAreUnique()
    {
        var state = TestHelpers.CreateState(treasureIndex: 1, trapIndex: 3);

        for (int seed = 0; seed < 30; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            var parts = text.Split("Door ");
            var doors = new List<char>();
            for (int i = 1; i < parts.Length; i++)
            {
                doors.Add(parts[i][0]);
            }

            Assert.Equal(doors.Count, doors.Distinct().Count());
        }
    }
}
