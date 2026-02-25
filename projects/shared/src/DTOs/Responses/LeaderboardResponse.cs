using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.DTOs.Responses;

public class LeaderboardResponse
{
    public List<LeaderboardEntry> Entries { get; set; } = new();
}
