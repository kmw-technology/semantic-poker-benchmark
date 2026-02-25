using SemanticPoker.Shared.Models;

namespace SemanticPoker.Shared.DTOs.Responses;

public class ModelListResponse
{
    public List<ModelInfo> Models { get; set; } = new();
}
