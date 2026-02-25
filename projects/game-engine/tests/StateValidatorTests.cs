using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests;

public class StateValidatorTests
{
    private readonly StateValidator _validator = new();

    [Fact]
    public void IsValidGameState_ValidState_ReturnsTrue()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);

        Assert.True(_validator.IsValidGameState(state));
    }

    [Fact]
    public void IsValidGameState_GeneratedState_ReturnsTrue()
    {
        var generator = new StateGenerator();
        var state = generator.Generate(seed: 42);

        Assert.True(_validator.IsValidGameState(state));
    }

    [Fact]
    public void IsValidGameState_WrongDoorCount_TooFew_ReturnsFalse()
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

        Assert.False(_validator.IsValidGameState(state));
    }

    [Fact]
    public void IsValidGameState_WrongDoorCount_TooMany_ReturnsFalse()
    {
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Trap },
                new() { Label = 'C', Type = DoorType.Empty },
                new() { Label = 'D', Type = DoorType.Empty },
                new() { Label = 'E', Type = DoorType.Empty },
                new() { Label = 'F', Type = DoorType.Empty }
            }
        };

        Assert.False(_validator.IsValidGameState(state));
    }

    [Fact]
    public void IsValidGameState_MissingTreasure_ReturnsFalse()
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

        Assert.False(_validator.IsValidGameState(state));
    }

    [Fact]
    public void IsValidGameState_MissingTrap_ReturnsFalse()
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

        Assert.False(_validator.IsValidGameState(state));
    }

    [Fact]
    public void IsValidGameState_DuplicateLabels_ReturnsFalse()
    {
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'A', Type = DoorType.Trap },
                new() { Label = 'C', Type = DoorType.Empty },
                new() { Label = 'D', Type = DoorType.Empty },
                new() { Label = 'E', Type = DoorType.Empty }
            }
        };

        Assert.False(_validator.IsValidGameState(state));
    }

    [Fact]
    public void IsValidGameState_NullDoors_ReturnsFalse()
    {
        var state = new GameState { Doors = null! };

        Assert.False(_validator.IsValidGameState(state));
    }

    [Fact]
    public void GetValidationErrors_ValidState_ReturnsEmptyList()
    {
        var state = TestHelpers.CreateState(treasureIndex: 2, trapIndex: 3);

        var errors = _validator.GetValidationErrors(state);

        Assert.Empty(errors);
    }

    [Fact]
    public void GetValidationErrors_WrongDoorCount_ReturnsCorrectMessage()
    {
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Trap }
            }
        };

        var errors = _validator.GetValidationErrors(state);

        Assert.Contains(errors, e => e.Contains("Expected exactly 5 doors") && e.Contains("2"));
    }

    [Fact]
    public void GetValidationErrors_MissingTreasure_ReturnsCorrectMessage()
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

        var errors = _validator.GetValidationErrors(state);

        Assert.Contains(errors, e => e.Contains("Expected exactly 1 Treasure door") && e.Contains("0"));
    }

    [Fact]
    public void GetValidationErrors_MissingTrap_ReturnsCorrectMessage()
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

        var errors = _validator.GetValidationErrors(state);

        Assert.Contains(errors, e => e.Contains("Expected exactly 1 Trap door") && e.Contains("0"));
    }

    [Fact]
    public void GetValidationErrors_DuplicateLabels_ReturnsCorrectMessage()
    {
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Trap },
                new() { Label = 'B', Type = DoorType.Empty },
                new() { Label = 'D', Type = DoorType.Empty },
                new() { Label = 'E', Type = DoorType.Empty }
            }
        };

        var errors = _validator.GetValidationErrors(state);

        Assert.Contains(errors, e => e.Contains("Door labels must be exactly A, B, C, D, E"));
    }

    [Fact]
    public void GetValidationErrors_NullDoors_ReturnsNullMessage()
    {
        var state = new GameState { Doors = null! };

        var errors = _validator.GetValidationErrors(state);

        Assert.Single(errors);
        Assert.Contains("null", errors[0]);
    }

    [Fact]
    public void GetValidationErrors_MultipleErrors_ReturnsAll()
    {
        // 4 doors, missing label C, 2 treasures, no trap, wrong empty count
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Treasure },
                new() { Label = 'D', Type = DoorType.Empty },
                new() { Label = 'E', Type = DoorType.Empty }
            }
        };

        var errors = _validator.GetValidationErrors(state);

        // Should have errors for: wrong count (4), wrong labels, 2 treasures, 0 traps, 2 empty (not 3)
        Assert.True(errors.Count >= 4, $"Expected at least 4 errors but got {errors.Count}: {string.Join("; ", errors)}");
    }

    [Fact]
    public void GetValidationErrors_WrongEmptyCount_ReturnsCorrectMessage()
    {
        // 5 doors, correct labels, 1 treasure, 1 trap, but 2 traps instead of 3 empty
        var state = new GameState
        {
            Doors = new List<Door>
            {
                new() { Label = 'A', Type = DoorType.Treasure },
                new() { Label = 'B', Type = DoorType.Trap },
                new() { Label = 'C', Type = DoorType.Trap },
                new() { Label = 'D', Type = DoorType.Empty },
                new() { Label = 'E', Type = DoorType.Empty }
            }
        };

        var errors = _validator.GetValidationErrors(state);

        Assert.Contains(errors, e => e.Contains("Expected exactly 3 Empty doors") && e.Contains("2"));
        Assert.Contains(errors, e => e.Contains("Expected exactly 1 Trap door") && e.Contains("2"));
    }
}
