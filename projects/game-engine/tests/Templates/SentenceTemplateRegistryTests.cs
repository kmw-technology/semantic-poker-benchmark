using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.GameEngine.Tests.Templates;

public class SentenceTemplateRegistryTests
{
    private readonly SentenceTemplateRegistry _registry = new();

    [Fact]
    public void AllTemplates_Returns13Templates()
    {
        Assert.Equal(13, _registry.AllTemplates.Count);
    }

    [Fact]
    public void AllTemplates_HaveUniqueIds()
    {
        var ids = _registry.AllTemplates.Select(t => t.TemplateId).ToHashSet();
        Assert.Equal(_registry.AllTemplates.Count, ids.Count);
    }

    [Fact]
    public void AllTemplates_ContainsExpectedTemplateIds()
    {
        var expectedIds = new[]
        {
            "negative-door", "pair-exclusion", "triplet-contains",
            "positional-relation", "adjacent-door", "same-side",
            "gap", "endpoint", "middle-door", "empty-neighbor",
            "order", "exact-empty", "qualitative-distance"
        };

        var actualIds = _registry.AllTemplates.Select(t => t.TemplateId).ToHashSet();

        foreach (var id in expectedIds)
        {
            Assert.Contains(id, actualIds);
        }
    }

    [Fact]
    public void GetApplicableTemplates_ValidState_ReturnsMultiple()
    {
        var state = TestHelpers.CreateState(treasureIndex: 0, trapIndex: 4);

        var applicable = _registry.GetApplicableTemplates(state);

        Assert.True(applicable.Count > 0);
    }

    [Fact]
    public void GetApplicableTemplates_SameSideState_IncludesSameSide()
    {
        var state = TestHelpers.CreateBothLeftState();

        var applicable = _registry.GetApplicableTemplates(state);
        var ids = applicable.Select(t => t.TemplateId).ToList();

        Assert.Contains("same-side", ids);
    }

    [Fact]
    public void GetApplicableTemplates_OppositeSideState_ExcludesSameSide()
    {
        var state = TestHelpers.CreateFarApartState(); // A and E = opposite sides

        var applicable = _registry.GetApplicableTemplates(state);
        var ids = applicable.Select(t => t.TemplateId).ToList();

        Assert.DoesNotContain("same-side", ids);
    }

    [Fact]
    public void GetTemplateById_ExistingId_ReturnsTemplate()
    {
        var template = _registry.GetTemplateById("negative-door");

        Assert.NotNull(template);
        Assert.Equal("negative-door", template!.TemplateId);
    }

    [Fact]
    public void GetTemplateById_NonExistingId_ReturnsNull()
    {
        var template = _registry.GetTemplateById("non-existent");

        Assert.Null(template);
    }
}
