using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests;

public class ScoreCalculatorTests
{
    private readonly ScoreCalculator _calculator = new();
    private const string ArchitectModel = "architect-model";

    [Fact]
    public void CalculateRound_PlayerPicksTreasure_PlayerGainsOneArchitectLosesOne()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-1", 'A') // picks Treasure
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(1, result.PlayerScoreChanges["player-1"]);
        Assert.Equal(-1, result.ArchitectScoreChange);
        Assert.Equal(1, decisions[0].ScoreChange);
        Assert.Equal(DoorType.Treasure, decisions[0].DoorOutcome);
    }

    [Fact]
    public void CalculateRound_PlayerPicksTrap_PlayerLosesOneArchitectGainsOne()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-1", 'E') // picks Trap
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(-1, result.PlayerScoreChanges["player-1"]);
        Assert.Equal(1, result.ArchitectScoreChange);
        Assert.Equal(-1, decisions[0].ScoreChange);
        Assert.Equal(DoorType.Trap, decisions[0].DoorOutcome);
    }

    [Fact]
    public void CalculateRound_PlayerPicksEmpty_BothScoreZero()
    {
        // Treasure at A(0), Trap at E(4), Empty = B,C,D
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-1", 'C') // picks Empty
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(0, result.PlayerScoreChanges["player-1"]);
        Assert.Equal(0, result.ArchitectScoreChange);
        Assert.Equal(0, decisions[0].ScoreChange);
        Assert.Equal(DoorType.Empty, decisions[0].DoorOutcome);
    }

    [Fact]
    public void CalculateRound_MultiplePlayers_MixedOutcomes()
    {
        // Treasure at A(0), Trap at E(4), Empty = B,C,D
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-treasure", 'A'), // Treasure: +1
            TestHelpers.CreateDecision("player-trap", 'E'),     // Trap: -1
            TestHelpers.CreateDecision("player-empty", 'C')     // Empty: 0
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(1, result.PlayerScoreChanges["player-treasure"]);
        Assert.Equal(-1, result.PlayerScoreChanges["player-trap"]);
        Assert.Equal(0, result.PlayerScoreChanges["player-empty"]);

        // Architect: -1 (from treasure pick) + 1 (from trap pick) + 0 = 0
        Assert.Equal(0, result.ArchitectScoreChange);
    }

    [Fact]
    public void CalculateRound_AllPlayersPickTreasure()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-1", 'A'),
            TestHelpers.CreateDecision("player-2", 'A'),
            TestHelpers.CreateDecision("player-3", 'A')
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(1, result.PlayerScoreChanges["player-1"]);
        Assert.Equal(1, result.PlayerScoreChanges["player-2"]);
        Assert.Equal(1, result.PlayerScoreChanges["player-3"]);

        // Architect loses 1 for each treasure pick: -3
        Assert.Equal(-3, result.ArchitectScoreChange);
    }

    [Fact]
    public void CalculateRound_AllPlayersPickTrap()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-1", 'E'),
            TestHelpers.CreateDecision("player-2", 'E'),
            TestHelpers.CreateDecision("player-3", 'E')
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(-1, result.PlayerScoreChanges["player-1"]);
        Assert.Equal(-1, result.PlayerScoreChanges["player-2"]);
        Assert.Equal(-1, result.PlayerScoreChanges["player-3"]);

        // Architect gains 1 for each trap pick: +3
        Assert.Equal(3, result.ArchitectScoreChange);
    }

    [Fact]
    public void CalculateRound_InvalidDoorChoice_ScoresZero()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-1", 'Z') // invalid door
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(0, result.PlayerScoreChanges["player-1"]);
        Assert.Equal(0, result.ArchitectScoreChange);
        Assert.Equal(0, decisions[0].ScoreChange);
        Assert.Equal(DoorType.Empty, decisions[0].DoorOutcome);
    }

    [Fact]
    public void CalculateRound_NoDecisions_ArchitectScoreZero()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>();

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(0, result.ArchitectScoreChange);
        Assert.Empty(result.PlayerScoreChanges);
    }

    [Fact]
    public void CalculateRound_SamePlayerMultipleDecisions_ScoresAccumulate()
    {
        // Treasure at A(0), Trap at E(4)
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);
        var decisions = new List<PlayerDecision>
        {
            TestHelpers.CreateDecision("player-1", 'A'), // Treasure: +1
            TestHelpers.CreateDecision("player-1", 'E')  // Trap: -1
        };

        var result = _calculator.CalculateRound(state, decisions, ArchitectModel);

        // Player-1's scores accumulate: +1 + (-1) = 0
        Assert.Equal(0, result.PlayerScoreChanges["player-1"]);
        // Architect: -1 (treasure) + 1 (trap) = 0
        Assert.Equal(0, result.ArchitectScoreChange);
    }

    [Fact]
    public void CalculateRound_UpdatesDecisionProperties()
    {
        // Treasure at B(1), Trap at D(3)
        var state = TestHelpers.CreateState(treasureIndex: 1, trapIndex: 3);
        var decision = TestHelpers.CreateDecision("player-1", 'B');
        var decisions = new List<PlayerDecision> { decision };

        _calculator.CalculateRound(state, decisions, ArchitectModel);

        Assert.Equal(1, decision.ScoreChange);
        Assert.Equal(DoorType.Treasure, decision.DoorOutcome);
    }
}
