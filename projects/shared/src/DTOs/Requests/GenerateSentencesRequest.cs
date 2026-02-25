namespace SemanticPoker.Shared.DTOs.Requests;

public class GenerateSentencesRequest
{
    public int? Seed { get; set; }
    public int Count { get; set; } = 2;
    public char? TreasureDoor { get; set; }
    public char? TrapDoor { get; set; }
}
