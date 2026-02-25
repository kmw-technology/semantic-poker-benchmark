namespace SemanticPoker.Shared.Enums;

public enum RoundPhase
{
    NotStarted = 0,
    StateGenerated = 1,
    EngineSentencesGenerated = 2,
    ArchitectSentencesGenerated = 3,
    SentencesShuffled = 4,
    PlayersDeciding = 5,
    Scoring = 6,
    Completed = 7
}
