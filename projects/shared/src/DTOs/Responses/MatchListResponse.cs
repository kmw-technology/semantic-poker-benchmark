namespace SemanticPoker.Shared.DTOs.Responses;

public class MatchListResponse
{
    public List<MatchResponse> Matches { get; set; } = new();
}
