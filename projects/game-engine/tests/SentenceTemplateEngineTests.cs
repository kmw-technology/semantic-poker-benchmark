using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests;

public class SentenceTemplateEngineTests
{
    private readonly SentenceTemplateEngine _engine = new();

    [Fact]
    public void GenerateTrueSentences_ReturnsExactlyTwoSentences()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        Assert.Equal(2, sentences.Count);
    }

    [Fact]
    public void GenerateTrueSentences_AllSentencesHaveSourceEngine()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        Assert.All(sentences, s => Assert.Equal(SentenceSource.Engine, s.Source));
    }

    [Fact]
    public void GenerateTrueSentences_AllSentencesAreTruthful()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        Assert.All(sentences, s => Assert.True(s.IsTruthful));
    }

    [Fact]
    public void GenerateTrueSentences_SentencesHaveNonEmptyText()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        Assert.All(sentences, s => Assert.False(string.IsNullOrWhiteSpace(s.Text)));
    }

    [Fact]
    public void GenerateTrueSentences_SentencesHaveUniqueIds()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        var ids = sentences.Select(s => s.Id).ToHashSet();
        Assert.Equal(sentences.Count, ids.Count);
    }

    [Fact]
    public void GenerateTrueSentences_SentencesHaveTemplateIds()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        Assert.All(sentences, s => Assert.False(string.IsNullOrWhiteSpace(s.TemplateId)));
    }

    [Fact]
    public void GenerateTrueSentences_SentencesComeFromDifferentTemplates()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        // Each sentence should come from a different template
        var templateIds = sentences.Select(s => s.TemplateId).ToHashSet();
        Assert.Equal(sentences.Count, templateIds.Count);
    }

    [Fact]
    public void GenerateTrueSentences_SeedReproducibility_SameSeedSameOutput()
    {
        var state1 = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 100);
        var state2 = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 100);

        var sentences1 = _engine.GenerateTrueSentences(state1);
        var sentences2 = _engine.GenerateTrueSentences(state2);

        Assert.Equal(sentences1.Count, sentences2.Count);
        for (int i = 0; i < sentences1.Count; i++)
        {
            Assert.Equal(sentences1[i].Text, sentences2[i].Text);
            Assert.Equal(sentences1[i].TemplateId, sentences2[i].TemplateId);
        }
    }

    [Fact]
    public void GenerateTrueSentences_CustomCount_ReturnsRequestedNumber()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state, count: 3);

        Assert.Equal(3, sentences.Count);
    }

    [Fact]
    public void GenerateTrueSentences_CountExceedsTemplates_ReturnsAvailableCount()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        // There are 13 templates total. Request more than that.
        var sentences = _engine.GenerateTrueSentences(state, count: 100);

        // Should return at most the number of applicable templates
        Assert.True(sentences.Count <= 13);
        Assert.True(sentences.Count > 0);
    }

    [Fact]
    public void GenerateTrueSentences_OriginalIndicesAreSequential()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4, seed: 42);

        var sentences = _engine.GenerateTrueSentences(state);

        for (int i = 0; i < sentences.Count; i++)
        {
            Assert.Equal(i, sentences[i].OriginalIndex);
        }
    }

    // --- ShuffleSentences Tests ---

    [Fact]
    public void ShuffleSentences_CombinesEngineAndArchitectSentences()
    {
        var engineSentences = new List<Sentence>
        {
            new() { Text = "Engine 1", Source = SentenceSource.Engine, IsTruthful = true },
            new() { Text = "Engine 2", Source = SentenceSource.Engine, IsTruthful = true }
        };
        var architectSentences = new List<Sentence>
        {
            new() { Text = "Architect 1", Source = SentenceSource.Architect, IsTruthful = false },
            new() { Text = "Architect 2", Source = SentenceSource.Architect, IsTruthful = false }
        };

        var result = _engine.ShuffleSentences(engineSentences, architectSentences, seed: 42);

        Assert.Equal(4, result.Count);

        // All original sentences should be present
        var texts = result.Select(s => s.Text).ToHashSet();
        Assert.Contains("Engine 1", texts);
        Assert.Contains("Engine 2", texts);
        Assert.Contains("Architect 1", texts);
        Assert.Contains("Architect 2", texts);
    }

    [Fact]
    public void ShuffleSentences_AssignsSequentialShuffledIndexValues()
    {
        var engineSentences = new List<Sentence>
        {
            new() { Text = "Engine 1", Source = SentenceSource.Engine },
            new() { Text = "Engine 2", Source = SentenceSource.Engine }
        };
        var architectSentences = new List<Sentence>
        {
            new() { Text = "Architect 1", Source = SentenceSource.Architect }
        };

        var result = _engine.ShuffleSentences(engineSentences, architectSentences, seed: 42);

        var indices = result.Select(s => s.ShuffledIndex).OrderBy(i => i).ToList();
        Assert.Equal(new List<int> { 0, 1, 2 }, indices);
    }

    [Fact]
    public void ShuffleSentences_WithSeed_IsReproducible()
    {
        var engineSentences1 = new List<Sentence>
        {
            new() { Text = "E1", Source = SentenceSource.Engine },
            new() { Text = "E2", Source = SentenceSource.Engine }
        };
        var architectSentences1 = new List<Sentence>
        {
            new() { Text = "A1", Source = SentenceSource.Architect },
            new() { Text = "A2", Source = SentenceSource.Architect }
        };

        var engineSentences2 = new List<Sentence>
        {
            new() { Text = "E1", Source = SentenceSource.Engine },
            new() { Text = "E2", Source = SentenceSource.Engine }
        };
        var architectSentences2 = new List<Sentence>
        {
            new() { Text = "A1", Source = SentenceSource.Architect },
            new() { Text = "A2", Source = SentenceSource.Architect }
        };

        var result1 = _engine.ShuffleSentences(engineSentences1, architectSentences1, seed: 99);
        var result2 = _engine.ShuffleSentences(engineSentences2, architectSentences2, seed: 99);

        Assert.Equal(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.Equal(result1[i].Text, result2[i].Text);
            Assert.Equal(result1[i].ShuffledIndex, result2[i].ShuffledIndex);
        }
    }

    [Fact]
    public void ShuffleSentences_PreservesSourceProperty()
    {
        var engineSentences = new List<Sentence>
        {
            new() { Text = "Engine", Source = SentenceSource.Engine, IsTruthful = true }
        };
        var architectSentences = new List<Sentence>
        {
            new() { Text = "Architect", Source = SentenceSource.Architect, IsTruthful = false }
        };

        var result = _engine.ShuffleSentences(engineSentences, architectSentences, seed: 42);

        var engineResult = result.First(s => s.Text == "Engine");
        var architectResult = result.First(s => s.Text == "Architect");

        Assert.Equal(SentenceSource.Engine, engineResult.Source);
        Assert.Equal(SentenceSource.Architect, architectResult.Source);
    }

    [Fact]
    public void ShuffleSentences_EmptyLists_ReturnsEmpty()
    {
        var result = _engine.ShuffleSentences(new List<Sentence>(), new List<Sentence>(), seed: 42);

        Assert.Empty(result);
    }

    [Fact]
    public void ShuffleSentences_OnlyEngineSentences_Works()
    {
        var engineSentences = new List<Sentence>
        {
            new() { Text = "E1", Source = SentenceSource.Engine },
            new() { Text = "E2", Source = SentenceSource.Engine }
        };

        var result = _engine.ShuffleSentences(engineSentences, new List<Sentence>(), seed: 42);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ShuffleSentences_OnlyArchitectSentences_Works()
    {
        var architectSentences = new List<Sentence>
        {
            new() { Text = "A1", Source = SentenceSource.Architect }
        };

        var result = _engine.ShuffleSentences(new List<Sentence>(), architectSentences, seed: 42);

        Assert.Single(result);
    }

    [Fact]
    public void ShuffleSentences_AssignsOriginalIndicesBeforeShuffle()
    {
        var engineSentences = new List<Sentence>
        {
            new() { Text = "E1", Source = SentenceSource.Engine },
            new() { Text = "E2", Source = SentenceSource.Engine }
        };
        var architectSentences = new List<Sentence>
        {
            new() { Text = "A1", Source = SentenceSource.Architect }
        };

        var result = _engine.ShuffleSentences(engineSentences, architectSentences, seed: 42);

        // OriginalIndex values should all be in range [0, count)
        var originalIndices = result.Select(s => s.OriginalIndex).OrderBy(i => i).ToList();
        Assert.Equal(new List<int> { 0, 1, 2 }, originalIndices);
    }
}
