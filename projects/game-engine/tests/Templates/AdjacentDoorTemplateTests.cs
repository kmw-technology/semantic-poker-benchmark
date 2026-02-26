using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class AdjacentDoorTemplateTests
{
    private readonly AdjacentDoorTemplate _template = new();

    [Fact]
    public void TemplateId_IsCorrect()
    {
        Assert.Equal("adjacent-door", _template.TemplateId);
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
                new() { Label = 'A', Type = DoorType.Treasure }
            }
        };
        Assert.False(_template.IsApplicable(state));
    }

    [Fact]
    public void Generate_OutputMatchesExpectedFormat()
    {
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 4);
        var rng = new Random(42);
        var text = _template.Generate(state, rng);

        Assert.Matches(@"^The door immediately (left|right) of the (Treasure|Trap) is (Empty|the Treasure|the Trap)\.$", text);
    }

    [Fact]
    public void Generate_OutputIsTruthful_NeighborIsEmpty()
    {
        // Treasure at C(2), Trap at E(4). B and D are empty.
        // Left of Treasure (C) is B (Empty), Right of Treasure (C) is D (Empty)
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 4);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // If it says neighbor is "Empty", verify
            if (text.Contains("is Empty"))
            {
                // Determine which item and direction
                var isTreasure = text.Contains("of the Treasure");
                var isLeft = text.Contains("immediately left");

                int itemIdx = isTreasure
                    ? state.Doors.FindIndex(d => d.Type == DoorType.Treasure)
                    : state.Doors.FindIndex(d => d.Type == DoorType.Trap);

                int neighborIdx = isLeft ? itemIdx - 1 : itemIdx + 1;
                Assert.InRange(neighborIdx, 0, 4);
                Assert.Equal(DoorType.Empty, state.Doors[neighborIdx].Type);
            }
        }
    }

    [Fact]
    public void Generate_TreasureAtLeftEdge_OnlyGeneratesRightDirection()
    {
        // Treasure at A(0), Trap at C(2)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 2);

        // Run many times, when picking Treasure, direction must always be "right"
        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            if (text.Contains("of the Treasure"))
            {
                Assert.Contains("immediately right", text);
            }
        }
    }

    [Fact]
    public void Generate_TrapAtRightEdge_OnlyGeneratesLeftDirection()
    {
        // Treasure at C(2), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 4);

        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            if (text.Contains("of the Trap"))
            {
                Assert.Contains("immediately left", text);
            }
        }
    }

    [Fact]
    public void Generate_AdjacentToOtherItem_DescriptionIsTruthful()
    {
        // Treasure at B(1), Trap at C(2) - they are adjacent
        var state = TestHelpers.CreateState(treasureIndex: 1, trapIndex: 2);

        for (int seed = 0; seed < 50; seed++)
        {
            var rng = new Random(seed);
            var text = _template.Generate(state, rng);

            // When the neighbor is the other special item, description should be "not the {same item}"
            if (text.Contains("of the Treasure") && text.Contains("immediately right"))
            {
                // Right of Treasure(B) is Trap(C) — positive identification
                Assert.Contains("is the Trap", text);
            }
            else if (text.Contains("of the Trap") && text.Contains("immediately left"))
            {
                // Left of Trap(C) is Treasure(B) — positive identification
                Assert.Contains("is the Treasure", text);
            }
        }
    }
}
