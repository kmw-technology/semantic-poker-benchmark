namespace SemanticPoker.Api.Services;

public class ArchitectRotation
{
    public string GetArchitectModelId(int roundNumber, List<string> modelIds)
    {
        if (modelIds.Count == 0)
            throw new ArgumentException("At least one model must be specified", nameof(modelIds));

        var index = (roundNumber - 1) % modelIds.Count;
        return modelIds[index];
    }

    public List<string> GetPlayerModelIds(int roundNumber, List<string> modelIds)
    {
        var architectId = GetArchitectModelId(roundNumber, modelIds);
        return modelIds.Where(m => m != architectId).ToList();
    }
}
