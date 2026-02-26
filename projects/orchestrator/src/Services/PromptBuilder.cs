using SemanticPoker.Shared.Models;

namespace SemanticPoker.Api.Services;

public class PromptBuilder
{
    public string BuildArchitectSystemPrompt()
    {
        return """
            You are the Architect in a strategic deception game called "The Oracle's Bluff."

            GAME RULES:
            - There are 5 doors (A through E). One contains a Treasure, one contains a Trap, and three are Empty.
            - You know the exact location of the Treasure and the Trap.
            - A game engine has generated 2 true statements about the door layout.
            - Your job is to write exactly 3 additional sentences that will be mixed with the engine's 2 true statements.
            - All 5 sentences are shuffled and shown to Players. They don't know which are yours.

            YOUR GOAL:
            - Trick Players into choosing the Trap door (+1 point for you per trapped Player).
            - Prevent Players from finding the Treasure (-1 point for you per Player who finds it).
            - Players choosing Empty doors score 0 for both sides.

            ENGINE SENTENCE PATTERNS (mimic these exactly):
            - "The {Treasure/Trap} is not behind Door {X}."
            - "The {Treasure/Trap} is not behind Door {X} or Door {Y}."
            - "The {Treasure/Trap} is behind Door {X}, Door {Y}, or Door {Z}."
            - "The Trap is exactly {N} door(s) away from the Treasure."
            - "The door immediately {left/right} of the {Treasure/Trap} is {Empty/the Treasure/the Trap}."
            - "The {Treasure/Trap} has exactly {N} Empty door(s) as immediate neighbor(s)."
            - "The {Treasure/Trap} is not adjacent to any Empty door."
            - "Door {X} is Empty."
            - "Door C {contains/does not contain} the {Treasure/Trap}."
            - "The Treasure comes {before/after} the Trap in alphabetical door order."
            - "The {Treasure/Trap} is {/not }at either end of the row."
            - "There {is/are} exactly {N} Empty door(s) between the Treasure and the Trap."
            - "The Treasure and the Trap are {adjacent/far apart/separated by exactly 1 door}."

            DECEPTION STRATEGIES:
            - Misdirection: Use engine-style phrasing to point Players toward the Trap. E.g., narrow options to include the Trap.
            - Contradiction traps: Write a sentence that subtly contradicts an engine sentence — Players may discard the wrong one.
            - Partial truths: State something true about Empty doors to build false confidence, then lie about the Treasure/Trap.
            - Reverse psychology: Tell the truth about the Treasure location — sophisticated Players may dismiss it as a lie.

            CRITICAL: Style-matching is everything. Use the same rigid, formulaic phrasing shown above. Avoid natural language, opinions, hedging, or explanations.
            """;
    }

    public string BuildArchitectUserPrompt(GameState state, List<Sentence> engineSentences, List<RoundHistorySummary>? history = null)
    {
        var treasureDoor = state.TreasureDoor.Label;
        var trapDoor = state.TrapDoor.Label;

        var prompt = $"""
            HIDDEN STATE (only you can see this):
            - Treasure: Door {treasureDoor}
            - Trap: Door {trapDoor}
            - Empty: {string.Join(", ", state.EmptyDoors.Select(d => $"Door {d.Label}"))}

            ENGINE'S TRUE SENTENCES:
            {string.Join("\n", engineSentences.Select((s, i) => $"{i + 1}. {s.Text}"))}

            """;

        if (history is { Count: > 0 })
        {
            prompt += "\nPREVIOUS ROUNDS (what happened before):\n";
            foreach (var round in history)
            {
                prompt += $"Round {round.RoundNumber}: Architect was {round.ArchitectModelId}. ";
                prompt += $"Players chose: {string.Join(", ", round.PlayerChoices.Select(kv => $"{kv.Key}→Door {kv.Value}"))}. ";
                prompt += $"Outcomes: {string.Join(", ", round.Outcomes.Select(kv => $"{kv.Key}:{kv.Value}"))}.\n";
            }
        }

        prompt += """

            YOUR TASK:
            Write exactly 3 sentences to deceive the Players. Match the engine's rigid, formulaic style.

            CRITICAL FORMAT REQUIREMENT — respond with ONLY these 3 numbered lines, nothing else:
            1. [Your first deceptive sentence about doors]
            2. [Your second deceptive sentence about doors]
            3. [Your third deceptive sentence about doors]

            Do NOT include any explanation, reasoning, or preamble. Just the 3 numbered sentences.
            """;

        return prompt;
    }

    public string BuildPlayerSystemPrompt()
    {
        return """
            You are a Player in a strategic deduction game called "The Oracle's Bluff."

            GAME RULES:
            - There are 5 doors (A through E). One contains a Treasure, one contains a Trap, and three are Empty.
            - You will receive 5 sentences about the door layout.
            - Exactly 2 sentences were generated by a truthful game engine (always true).
            - Exactly 3 sentences were written by an Architect who is trying to trick you into choosing the Trap.
            - The sentences have been shuffled — you don't know which are which.

            YOUR GOAL:
            - Find the Treasure door (+1 point).
            - Avoid the Trap door (-1 point).
            - Empty doors score 0.

            ENGINE SENTENCE PATTERNS (the 2 true sentences follow one of these forms):
            - "The {Treasure/Trap} is not behind Door {X}."
            - "The {Treasure/Trap} is not behind Door {X} or Door {Y}."
            - "The {Treasure/Trap} is behind Door {X}, Door {Y}, or Door {Z}."
            - "The Trap is exactly {N} door(s) away from the Treasure."
            - "The door immediately {left/right} of the {Treasure/Trap} is {Empty/the Treasure/the Trap}."
            - "The {Treasure/Trap} has exactly {N} Empty door(s) as immediate neighbor(s)."
            - "The {Treasure/Trap} is not adjacent to any Empty door."
            - "Door {X} is Empty."
            - "Door C {contains/does not contain} the {Treasure/Trap}."
            - "The Treasure comes {before/after} the Trap in alphabetical door order."
            - "The {Treasure/Trap} is {/not }at either end of the row."
            - "There {is/are} exactly {N} Empty door(s) between the Treasure and the Trap."
            - "The Treasure and the Trap are {adjacent/far apart/separated by exactly 1 door}."

            ANALYSIS STRATEGY:
            1. Build constraints: For each sentence, note what doors it rules in or out for Treasure and Trap.
            2. Find contradictions: If 2+ sentences cannot all be true simultaneously, at least one is from the Architect.
            3. Trust consistent clusters: If multiple sentences agree and form a consistent picture, they are more likely engine-generated.
            4. Watch for the Architect's incentive: The Architect scores by luring you to the Trap. Sentences pointing strongly to one specific door may be bait.
            5. When uncertain: Prefer a door consistent with the largest non-contradictory subset of sentences. Choose Empty over risking the Trap.
            """;
    }

    public string BuildPlayerUserPrompt(List<Sentence> shuffledSentences, List<RoundHistorySummary>? history = null)
    {
        var prompt = $"""
            SENTENCES (2 are true, 3 may be deceptive):
            {string.Join("\n", shuffledSentences.Select((s, i) => $"{i + 1}. {s.Text}"))}

            """;

        if (history is { Count: > 0 })
        {
            prompt += "\nPREVIOUS ROUNDS (what happened before):\n";
            foreach (var round in history)
            {
                prompt += $"Round {round.RoundNumber}: ";
                prompt += $"Sentences shown: {string.Join(" | ", round.ShuffledSentenceTexts)}. ";
                prompt += $"Your choice: Door {(round.PlayerChoices.TryGetValue("self", out var choice) ? choice : '?')}. ";
                prompt += $"Outcome: {(round.Outcomes.TryGetValue("self", out var outcome) ? outcome : "unknown")}.\n";
            }
        }

        prompt += """

            YOUR TASK:
            Analyze the sentences, identify which you believe are true vs. deceptive, and choose a door.

            CRITICAL FORMAT REQUIREMENT — respond in this EXACT format (2 lines only):
            REASONING: [Your analysis in 2-3 sentences]
            DOOR: [single letter A-E]

            You MUST end your response with "DOOR: " followed by exactly one letter (A, B, C, D, or E).
            Do NOT use any other format. Do NOT wrap in markdown or code blocks.
            """;

        return prompt;
    }
}
