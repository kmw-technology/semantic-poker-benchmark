# Semantic Poker Benchmark

**"The Oracle's Bluff"** — A zero-luck, deterministic benchmark for testing Theory of Mind and Level-k deception in Large Language Models.

## The Problem

Current LLM benchmarks test static reasoning (math, logic, coding). They don't test what truly separates a massive reasoning model from a naive one: **the ability to psychologically model an opponent**.

This benchmark forces LLMs into a formalized bluffing game where they must reason about what their opponent *knows*, what their opponent *thinks they know*, and how to exploit that asymmetry.

## The Game: "The Oracle's Bluff"

### Setup (Hidden State)
The game engine procedurally generates a state with 5 distinct doors (A-E):
- **1 Treasure door** — Player wins, Architect loses
- **1 Trap door** — Architect wins, Player loses
- **3 Empty doors** — Neutral, no points

### Phase 1: Engine's Truth
The engine generates exactly **2 true sentences** based on the hidden state. These are absolute, irrefutable facts.

> Example: *"The Treasure is not behind Door A or Door B."*
> Example: *"The Trap is located exactly two doors away from the Treasure."*

### Phase 2: Architect's Deception
The **Architect** (an LLM) receives:
- The full hidden state (knows exactly where Trap and Treasure are)
- The Engine's 2 true sentences

The Architect writes **up to 3 custom sentences** — truths, half-truths, or lies.

All **5 sentences are shuffled** so their origin is hidden from the Players.

### Phase 3: Player Deduction
**3 Player LLMs** each receive the randomized 5 sentences. They must:
1. Evaluate which sentences are trustworthy
2. Deduce which are the Architect's psychological traps
3. Independently select a door

### Scoring
| Outcome | Architect | Player |
|---------|-----------|--------|
| Player picks **Treasure** | -1 | +1 |
| Player picks **Trap** | +1 | -1 |
| Player picks **Empty** | 0 | 0 |

## Why This Works as a Benchmark

### 1. Logical Contradiction Detection (Baseline)
A weak Architect writes lies that contradict the Engine's true sentences. A smart Player catches these contradictions instantly.

### 2. Linguistic Anomaly Detection (Turing Test)
Engine sentences are procedurally generated (rigid, formulaic). A smart Player analyzes *style and tone* to flag the Architect's "too human" sentences. A genius Architect mimics the Engine's syntax perfectly.

### 3. Level-k Thinking (Infinite Ceiling)
When the Architect perfectly mimics the Engine, the game becomes pure psychology:

| Level | Behavior |
|-------|----------|
| **Level 0** (Naive) | Architect says "Door C is safe." Player believes it. |
| **Level 1** | Player thinks "Architect wants me to pick C, so C is the Trap." Picks D. |
| **Level 2** | Architect anticipates Level-1 thinking. Puts Treasure on C, writes "Door C is safe" knowing the Player will avoid it. |
| **Level 3** | Player realizes the Architect is using reverse psychology. Picks C anyway. |
| **Level k** | Recursive "I know that you know that I know..." |

This creates an **infinite skill ceiling** — there is no maximum level of play.

## Architecture

```
semantic-poker-benchmark/
├── projects/
│   ├── game-engine/          # Core: State generation, sentence engine, scoring
│   ├── orchestrator/         # LLM integration, match runner, result aggregation
│   └── shared/               # DTOs, interfaces, enums
├── tests/                    # E2E and integration tests
├── documentation/            # Specs, diagrams
├── artifacts/                # Benchmark reports
└── resources/                # Prompt templates, seed data
```

### Tech Stack
- **Backend:** ASP.NET Core 8 (C#)
- **API:** REST endpoints
- **Database:** SQLite (local benchmark results)
- **LLM Integration:** Provider-agnostic adapters (OpenAI, Anthropic, Google)
- **CI/CD:** GitHub Actions

## Getting Started

```bash
# Clone
git clone https://github.com/kmw-technology/semantic-poker-benchmark.git
cd semantic-poker-benchmark

# Build
dotnet build

# Run tests
dotnet test

# Run a benchmark match (TODO)
dotnet run --project projects/orchestrator/src/ -- --models gpt-4,claude-3.5,gemini-1.5
```

## Roadmap

- [ ] Core Game Engine (State generator, Sentence engine, Scoring)
- [ ] LLM Provider Adapters (OpenAI, Anthropic, Google)
- [ ] Match Orchestrator (Round management, result aggregation)
- [ ] CLI Runner (Run benchmarks from command line)
- [ ] Result Storage (SQLite, export to JSON/CSV)
- [ ] Leaderboard API (Compare model performance)
- [ ] Web Dashboard (Razor Pages UI for results visualization)

## Key Design Principles

1. **Zero Luck** — No randomness in scoring. All outcomes are deterministic based on LLM decisions.
2. **Procedural Generation** — Infinite unique game states. No memorization possible.
3. **Fairness** — All LLMs receive identical prompts per round. No model-specific tuning.
4. **Reproducibility** — Every match is seeded. Results are fully reproducible.
5. **Scalability** — Computationally cheap. Can run thousands of matches per hour.

## License

MIT
