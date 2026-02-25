namespace SemanticPoker.Shared.DTOs.Responses;

public class DoorDto
{
    public char Label { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class GenerateStateResponse
{
    public List<DoorDto> Doors { get; set; } = new();
    public char TreasureDoor { get; set; }
    public char TrapDoor { get; set; }
    public int Seed { get; set; }
}
