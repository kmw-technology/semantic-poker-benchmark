using SemanticPoker.Shared.Enums;

namespace SemanticPoker.Shared.DTOs.Requests;

public class SubmitHumanInputRequest
{
    public PlayerRole InputType { get; set; }
    public List<string>? ArchitectSentences { get; set; }
    public char? ChosenDoor { get; set; }
    public string? Reasoning { get; set; }
}
