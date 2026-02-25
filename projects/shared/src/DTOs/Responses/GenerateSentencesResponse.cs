namespace SemanticPoker.Shared.DTOs.Responses;

public class GenerateSentencesResponse
{
    public GenerateStateResponse State { get; set; } = new();
    public List<SentenceDto> Sentences { get; set; } = new();
}
