namespace SemanticPoker.Shared.DTOs.Requests;

public class PlayerSlot
{
    public string Type { get; set; } = "LLM"; // "Human" or "LLM"
    public string? Name { get; set; }          // Required for Human
    public string? ModelId { get; set; }       // Required for LLM
}
