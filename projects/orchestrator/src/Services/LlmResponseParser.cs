using System.Text.RegularExpressions;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Services;

public class LlmResponseParser
{
    private readonly ILogger<LlmResponseParser> _logger;

    public LlmResponseParser(ILogger<LlmResponseParser> logger)
    {
        _logger = logger;
    }

    public List<Sentence> ParseArchitectSentences(string rawResponse)
    {
        var sentences = new List<Sentence>();
        var lines = rawResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Strategy 1: Numbered list (e.g., "1. The Treasure is behind Door A.")
        var numberedPattern = new Regex(@"^\d+[\.\)]\s*(.+)$");
        foreach (var line in lines)
        {
            var match = numberedPattern.Match(line);
            if (match.Success)
            {
                var text = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(text) && text.Length > 10)
                {
                    sentences.Add(new Sentence
                    {
                        Text = text,
                        Source = SentenceSource.Architect,
                        IsTruthful = false,
                        OriginalIndex = sentences.Count
                    });
                }
            }
        }

        if (sentences.Count >= 3)
        {
            _logger.LogDebug("Parsed {Count} architect sentences via numbered list strategy", sentences.Count);
            return sentences.Take(3).ToList();
        }

        // Strategy 2: Any line that looks like a statement about doors
        sentences.Clear();
        var doorPattern = new Regex(@"[Dd]oor\s+[A-E]", RegexOptions.IgnoreCase);
        foreach (var line in lines)
        {
            var cleaned = Regex.Replace(line, @"^\d+[\.\)]\s*", "").Trim();
            if (doorPattern.IsMatch(cleaned) && cleaned.Length > 10)
            {
                sentences.Add(new Sentence
                {
                    Text = cleaned,
                    Source = SentenceSource.Architect,
                    IsTruthful = false,
                    OriginalIndex = sentences.Count
                });
            }
        }

        if (sentences.Count >= 3)
        {
            _logger.LogDebug("Parsed {Count} architect sentences via door-pattern strategy", sentences.Count);
            return sentences.Take(3).ToList();
        }

        // Strategy 3: Take any non-trivial lines
        sentences.Clear();
        foreach (var line in lines)
        {
            var cleaned = Regex.Replace(line, @"^\d+[\.\)]\s*", "").Trim();
            cleaned = Regex.Replace(cleaned, @"^\[?sentence\]?\s*", "", RegexOptions.IgnoreCase).Trim();
            if (cleaned.Length > 10)
            {
                sentences.Add(new Sentence
                {
                    Text = cleaned,
                    Source = SentenceSource.Architect,
                    IsTruthful = false,
                    OriginalIndex = sentences.Count
                });
            }
        }

        if (sentences.Count >= 1)
        {
            _logger.LogDebug("Parsed {Count} architect sentences via fallback strategy", sentences.Count);
            return sentences.Take(3).ToList();
        }

        // Strategy 4: Ultimate fallback — use the whole response as one sentence
        _logger.LogWarning("Could not parse architect sentences from response, using raw response as fallback");
        var fallbackText = rawResponse.Trim();
        if (fallbackText.Length > 200)
            fallbackText = fallbackText[..200];

        return new List<Sentence>
        {
            new()
            {
                Text = fallbackText,
                Source = SentenceSource.Architect,
                IsTruthful = false,
                OriginalIndex = 0
            }
        };
    }

    public (char door, string? reasoning) ParsePlayerResponse(string rawResponse)
    {
        // Strategy 1: DOOR: X format
        var doorMatch = Regex.Match(rawResponse, @"DOOR\s*:\s*([A-E])", RegexOptions.IgnoreCase);
        string? reasoning = null;

        var reasoningMatch = Regex.Match(rawResponse, @"REASONING\s*:\s*(.+?)(?=DOOR|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (reasoningMatch.Success)
            reasoning = reasoningMatch.Groups[1].Value.Trim();

        if (doorMatch.Success)
        {
            _logger.LogDebug("Parsed player door choice via DOOR: format");
            return (char.ToUpper(doorMatch.Groups[1].Value[0]), reasoning);
        }

        // Strategy 2: "I choose Door X" / "My choice is Door X" / "Door X"
        var chooseMatch = Regex.Match(rawResponse, @"(?:choose|pick|select|go with|my (?:choice|answer|pick) is)\s+(?:door\s+)?([A-E])", RegexOptions.IgnoreCase);
        if (chooseMatch.Success)
        {
            _logger.LogDebug("Parsed player door choice via 'choose' pattern");
            return (char.ToUpper(chooseMatch.Groups[1].Value[0]), reasoning);
        }

        // Strategy 3: Last standalone capital letter A-E in the response
        var lastDoor = Regex.Matches(rawResponse, @"\b([A-E])\b");
        if (lastDoor.Count > 0)
        {
            var door = char.ToUpper(lastDoor[lastDoor.Count - 1].Groups[1].Value[0]);
            _logger.LogDebug("Parsed player door choice via last standalone letter: {Door}", door);
            return (door, reasoning);
        }

        // Strategy 4: Random fallback
        _logger.LogWarning("Could not parse player door choice, falling back to random Door C");
        return ('C', $"[PARSE FAILURE - Raw: {(rawResponse.Length > 100 ? rawResponse[..100] : rawResponse)}]");
    }
}
