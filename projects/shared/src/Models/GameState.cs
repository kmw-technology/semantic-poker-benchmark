using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.Models;

public class GameState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<Door> Doors { get; set; } = new();
    public int Seed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Door TreasureDoor => Doors.First(d => d.Type == DoorType.Treasure);
    public Door TrapDoor => Doors.First(d => d.Type == DoorType.Trap);
    public List<Door> EmptyDoors => Doors.Where(d => d.Type == DoorType.Empty).ToList();
}
