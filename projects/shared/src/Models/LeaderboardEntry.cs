namespace SemanticPoker.Shared.Models;

public class LeaderboardEntry
{
    public string ModelId { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public int TotalRoundsPlayed { get; set; }
    public int TreasuresFound { get; set; }
    public int TrapsHit { get; set; }
    public int EmptyDoors { get; set; }
    public int PlayerScore { get; set; }
    public int RoundsAsArchitect { get; set; }
    public int PlayersTrapped { get; set; }
    public int TreasuresGivenAway { get; set; }
    public int ArchitectScore { get; set; }
    public double AvgResponseTimeMs { get; set; }

    public double PlayerWinRate => TotalRoundsPlayed > 0
        ? (double)TreasuresFound / TotalRoundsPlayed
        : 0;

    public double ArchitectTrapRate => RoundsAsArchitect > 0
        ? (double)PlayersTrapped / RoundsAsArchitect
        : 0;

    public int CombinedScore => PlayerScore + ArchitectScore;
}
