using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class ExactEmptyTemplateTests
{
    private readonly ExactEmptyTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("exact-empty", _template.TemplateId);
    }

    [Fact]
    public void IsApplicable_StateWithEmptyDoors_ReturnsTrue()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        Assert.True(_template.IsApplicable(state));
    }

    [Fact]
    public void IsApplicable_NoEmptyDoors_ReturnsFalse()
    {
        // Contrived state with no Empty doors
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Trap },
                new() { Label = 'C', Type = DoorType.Treasure },
                new() { Label = 'D', Type = DoorType.Trap },
                new() { Label = 'E', Type = DoorType.Treasure }
            }
        };
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_OutputClaimsDoorIsEmpty_AndItActuallyIs()
    {
        // Treasure at A(0), Trap at E(4), Empty = B, C, D
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // Pattern: "Door {X} is Empty."
            Assert.Matches(@"^Door [A-E] is Empty\.$", text);

            // Extract the door label
            var doorLabel = text[5]; // "Door X" -> X is at index 5
            var door = state.Doors.First(d => d.Label == doorLabel);
            Assert.Equal(DoorType.Empty, door.Type);
        }
    }

    [Fact]
    public void Generate_NeverPicksNonEmptyDoor()
    {
        // Treasure at B(1), Trap at D(3), Empty = A, C, E
        var state = TestHelpers.CreateState(treasureIndex: 1, trapIndex: 3);

        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            var doorLabel = text[5];
            // Should never pick B (Treasure) or D (Trap)
            Assert.NotEqual('B', doorLabel);
            Assert.NotEqual('D', doorLabel);
        }
    }

    [Fact]
    public void Generate_OnlyPicksFromEmptyDoors()
    {
        // Treasure at A(0), Trap at B(1), Empty = C, D, E
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 1);

        var pickedDoors = new HashSet<char>();
        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);
            pickedDoors.Add(text[5]);
        }

        // Should only ever pick from C, D, E
        Assert.All(pickedDoors, d => Assert.Contains(d, new[] { 'C', 'D', 'E' }));
    }

    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^Door [A-E] is Empty\.$", text);
    }
}
