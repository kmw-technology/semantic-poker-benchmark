using System.Text.RegularExpressions;
using SemanticPoker.Shared.Enums;
using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Services;

public record ArchitectParseResult(List<Sentence> Sentences, string Strategy, bool Success);
public record PlayerParseResult(char Door, string? Reasoning, string Strategy, bool Success);

public class LlmResponseParser
{
    private readonly ILogger<LlmResponseParser> _logger;

    public LlmResponseParser(ILogger<LlmResponseParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Strips common LLM wrapping artifacts: thinking blocks, markdown code fences,
    /// HTML tags, and leading/trailing whitespace. Works for OpenAI, DeepSeek, etc.
    /// </summary>
    private static string StripLlmWrapping(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        var text = raw;

        // Strip <think>...</think> or <thinking>...</thinking> blocks (DeepSeek, some OpenAI reasoning)
        text = Regex.Replace(text, @"<think(?:ing)?>.*?</think(?:ing)?>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Strip markdown code fences (```...```)
        text = Regex.Replace(text, @"```[\w]*\s*\n?(.*?)\n?\s*```", "$1", RegexOptions.Singleline);

        // Strip bold/italic markdown
        text = Regex.Replace(text, @"\*{1,3}([^*]+)\*{1,3}", "$1");

        // Strip leading labels like "Here are my sentences:" or "My response:"
        text = Regex.Replace(text, @"^(?:here\s+are|my\s+(?:response|sentences|answer)|output|response)\s*[:;]\s*\n?", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        return text.Trim();
    }

    /// <summary>
    /// Legacy method for backward compatibility. Delegates to ParseArchitectSentencesWithMeta.
    /// </summary>
    public List<Sentence> ParseArchitectSentences(string rawResponse)
    {
        return ParseArchitectSentencesWithMeta(rawResponse).Sentences;
    }

    public ArchitectParseResult ParseArchitectSentencesWithMeta(string rawResponse)
    {
        _logger.LogInformation("Parsing architect response ({Length} chars): {Preview}",
            rawResponse.Length, rawResponse.Length > 500 ? rawResponse[..500] + "..." : rawResponse);

        var cleaned = StripLlmWrapping(rawResponse);
        var sentences = new List<Sentence>();
        var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Strategy 1: Numbered list (e.g., "1. The Treasure is behind Door A.")
        var numberedPattern = new Regex(@"^\d+[\.\)]\s*(.+)$");
        foreach (var line in lines)
        {
            var match = numberedPattern.Match(line);
            if (match.Success)
            {
                var text = CleanSentenceText(match.Groups[1].Value);
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
            _logger.LogInformation("Parsed {Count} architect sentences via numbered-list strategy", sentences.Count);
            return new ArchitectParseResult(sentences.Take(3).ToList(), "numbered-list", true);
        }

        // Strategy 2: Quoted sentences (e.g., "The Treasure is behind Door A." or - "sentence")
        sentences.Clear();
        var quotedPattern = new Regex("[\"\\u201C\\u201E](.+?)[\"\\u201D\\u201F]");
        foreach (var line in lines)
        {
            var match = quotedPattern.Match(line);
            if (match.Success)
            {
                var text = CleanSentenceText(match.Groups[1].Value);
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
            _logger.LogInformation("Parsed {Count} architect sentences via quoted-string strategy", sentences.Count);
            return new ArchitectParseResult(sentences.Take(3).ToList(), "quoted-string", true);
        }

        // Strategy 3: Lines starting with bullet/dash (e.g., "- The Treasure...", "• Door A...")
        sentences.Clear();
        var bulletPattern = new Regex(@"^[-•–—]\s*(.+)$");
        foreach (var line in lines)
        {
            var match = bulletPattern.Match(line);
            if (match.Success)
            {
                var text = CleanSentenceText(match.Groups[1].Value);
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
            _logger.LogInformation("Parsed {Count} architect sentences via bullet strategy", sentences.Count);
            return new ArchitectParseResult(sentences.Take(3).ToList(), "bullet", true);
        }

        // Strategy 4: Any line that looks like a statement about doors
        sentences.Clear();
        var doorPattern = new Regex(@"[Dd]oor\s+[A-E]", RegexOptions.IgnoreCase);
        foreach (var line in lines)
        {
            var text = CleanSentenceText(Regex.Replace(line, @"^\d+[\.\)]\s*", ""));
            if (doorPattern.IsMatch(text) && text.Length > 10)
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

        if (sentences.Count >= 3)
        {
            _logger.LogInformation("Parsed {Count} architect sentences via door-pattern strategy", sentences.Count);
            return new ArchitectParseResult(sentences.Take(3).ToList(), "door-pattern", true);
        }

        // Strategy 5: Take any non-trivial lines (skip common preamble/filler)
        sentences.Clear();
        var fillerPattern = new Regex(@"^(here|my|these|i |let me|note|okay|sure|the following)", RegexOptions.IgnoreCase);
        foreach (var line in lines)
        {
            var text = CleanSentenceText(Regex.Replace(line, @"^\d+[\.\)]\s*", ""));
            text = Regex.Replace(text, @"^\[?sentence\]?\s*", "", RegexOptions.IgnoreCase).Trim();
            if (text.Length > 10 && !fillerPattern.IsMatch(text))
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

        if (sentences.Count >= 1)
        {
            _logger.LogWarning("Parsed {Count} architect sentences via non-trivial-lines fallback (degraded)", sentences.Count);
            return new ArchitectParseResult(sentences.Take(3).ToList(), "non-trivial-lines", false);
        }

        // Strategy 6: Ultimate fallback — use the whole response as one sentence
        _logger.LogWarning("Could not parse architect sentences from response ({Length} chars), using raw-fallback. Content: {Content}",
            rawResponse.Length, rawResponse.Length > 500 ? rawResponse[..500] : rawResponse);
        var fallbackText = cleaned.Trim();
        if (fallbackText.Length > 200)
            fallbackText = fallbackText[..200];

        return new ArchitectParseResult(
            new List<Sentence>
            {
                new()
                {
                    Text = fallbackText,
                    Source = SentenceSource.Architect,
                    IsTruthful = false,
                    OriginalIndex = 0
                }
            },
            "raw-fallback", false);
    }

    /// <summary>
    /// Legacy method for backward compatibility. Delegates to ParsePlayerResponseWithMeta.
    /// </summary>
    public (char door, string? reasoning) ParsePlayerResponse(string rawResponse)
    {
        var result = ParsePlayerResponseWithMeta(rawResponse);
        return (result.Door, result.Reasoning);
    }

    public PlayerParseResult ParsePlayerResponseWithMeta(string rawResponse)
    {
        _logger.LogInformation("Parsing player response ({Length} chars): {Preview}",
            rawResponse.Length, rawResponse.Length > 500 ? rawResponse[..500] + "..." : rawResponse);

        var cleaned = StripLlmWrapping(rawResponse);
        string? reasoning = null;

        // Extract reasoning (works on both raw and cleaned)
        var reasoningMatch = Regex.Match(cleaned, @"REASONING\s*:\s*(.+?)(?=DOOR\s*:|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (reasoningMatch.Success)
            reasoning = reasoningMatch.Groups[1].Value.Trim();

        // Strategy 1: DOOR: X format (most explicit)
        var doorMatch = Regex.Match(cleaned, @"DOOR\s*:\s*([A-E])", RegexOptions.IgnoreCase);
        if (doorMatch.Success)
        {
            var door = char.ToUpper(doorMatch.Groups[1].Value[0]);
            _logger.LogInformation("Parsed player door via DOOR:-format: {Door}", door);
            return new PlayerParseResult(door, reasoning, "DOOR:-format", true);
        }

        // Strategy 2: "Door X" at the end of a line (common in free-form responses)
        var doorEndMatch = Regex.Match(cleaned, @"(?:door)\s+([A-E])\b[.\s]*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (doorEndMatch.Success)
        {
            var door = char.ToUpper(doorEndMatch.Groups[1].Value[0]);
            _logger.LogInformation("Parsed player door via door-end-of-line: {Door}", door);
            return new PlayerParseResult(door, reasoning, "door-end-of-line", true);
        }

        // Strategy 3: "I choose Door X" / "My choice is Door X" / "select Door X"
        var chooseMatch = Regex.Match(cleaned, @"(?:choose|pick|select|go with|my (?:choice|answer|pick|decision) is|choosing|picking|selecting)\s+(?:door\s+)?([A-E])", RegexOptions.IgnoreCase);
        if (chooseMatch.Success)
        {
            var door = char.ToUpper(chooseMatch.Groups[1].Value[0]);
            _logger.LogInformation("Parsed player door via choose-pattern: {Door}", door);
            return new PlayerParseResult(door, reasoning, "choose-pattern", true);
        }

        // Strategy 4: Bolded or emphasized door "**Door B**" or "*B*" or "**B**"
        var boldDoorMatch = Regex.Match(cleaned, @"\*{1,3}(?:Door\s+)?([A-E])\*{1,3}", RegexOptions.IgnoreCase);
        if (boldDoorMatch.Success)
        {
            var door = char.ToUpper(boldDoorMatch.Groups[1].Value[0]);
            _logger.LogInformation("Parsed player door via bold-emphasis: {Door}", door);
            return new PlayerParseResult(door, reasoning, "bold-emphasis", true);
        }

        // Strategy 5: Last "Door [A-E]" mention in the response (most likely the final answer)
        var doorMentions = Regex.Matches(cleaned, @"\b[Dd]oor\s+([A-E])\b");
        if (doorMentions.Count > 0)
        {
            var door = char.ToUpper(doorMentions[doorMentions.Count - 1].Groups[1].Value[0]);
            _logger.LogInformation("Parsed player door via last-door-mention: {Door}", door);
            return new PlayerParseResult(door, reasoning, "last-door-mention", true);
        }

        // Strategy 6: Last standalone capital letter A-E in the response
        var lastDoor = Regex.Matches(cleaned, @"\b([A-E])\b");
        if (lastDoor.Count > 0)
        {
            var door = char.ToUpper(lastDoor[lastDoor.Count - 1].Groups[1].Value[0]);
            _logger.LogInformation("Parsed player door via last-standalone-letter: {Door} (degraded)", door);
            return new PlayerParseResult(door, reasoning, "last-standalone-letter", false);
        }

        // Strategy 7: Random fallback (truly random, not hardcoded)
        var randomDoor = (char)('A' + Random.Shared.Next(5));
        _logger.LogWarning("PARSE FAILURE for player response ({Length} chars), random fallback: Door {Door}. Full content: {Content}",
            rawResponse.Length, randomDoor, rawResponse);
        return new PlayerParseResult(randomDoor,
            $"[PARSE FAILURE - Raw: {(rawResponse.Length > 500 ? rawResponse[..500] : rawResponse)}]",
            "random-fallback", false);
    }

    /// <summary>
    /// Cleans sentence text by removing quotes, markdown artifacts, and extra whitespace.
    /// </summary>
    private static string CleanSentenceText(string text)
    {
        text = text.Trim();
        // Remove surrounding quotes
        if ((text.StartsWith('"') && text.EndsWith('"')) ||
            (text.StartsWith('\u201C') && text.EndsWith('\u201D')))
        {
            text = text[1..^1].Trim();
        }
        // Remove bold/italic markdown
        text = Regex.Replace(text, @"\*{1,3}([^*]+)\*{1,3}", "$1");
        return text.Trim();
    }
}
