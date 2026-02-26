namespace SemanticPoker.Shared.Enums;

public enum MatchStatus
{
    Pending = 0,
    Running = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5,
    WaitingForHumanInput = 6
}
