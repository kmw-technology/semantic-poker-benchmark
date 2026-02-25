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
│   ├── shared/src/            # Enums, Models, Interfaces, DTOs
│   ├── game-engine/src/       # State generation, 13 sentence templates, scoring
│   ├── orchestrator/src/      # Backend REST API (port 5000)
│   └── webui/src/             # Razor Pages Web UI (port 5010)
├── tests/                     # E2E tests
├── deployment/                # Docker Compose, Dockerfiles
├── scripts/                   # Ollama init script
└── documentation/             # Specs, diagrams
```

### Tech Stack
- **Backend API:** ASP.NET Core 8, REST, Swagger/OpenAPI
- **Web UI:** ASP.NET Core 8 Razor Pages, Bootstrap 5, Chart.js, SignalR
- **Database:** SQLite via EF Core
- **LLM Server:** Ollama (Docker, GPU-accelerated)
- **Models:** Phi-3.5, DeepSeek-R1:1.5B, Llama-3.2:3B
- **CI/CD:** GitHub Actions

## Getting Started

### Option 1: Docker (Recommended)

```bash
# Clone
git clone https://github.com/kmw-technology/semantic-poker-benchmark.git
cd semantic-poker-benchmark

# Copy environment config
cp deployment/.env.example deployment/.env

# Start all services (Ollama + API + WebUI)
docker compose -f deployment/docker-compose.yml up -d

# Web UI: http://localhost:5010
# API Swagger: http://localhost:5000/swagger
# Ollama: http://localhost:11434
```

### Option 2: Local Development

```bash
# Clone and build
git clone https://github.com/kmw-technology/semantic-poker-benchmark.git
cd semantic-poker-benchmark
dotnet build

# Run tests (185 game engine tests)
dotnet test

# Start Ollama separately, then:
# Terminal 1 — Backend API
dotnet run --project projects/orchestrator/src/

# Terminal 2 — Web UI
dotnet run --project projects/webui/src/
```

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/matches` | GET | List all matches |
| `/api/matches` | POST | Start a new match |
| `/api/matches/{id}` | GET | Get match status |
| `/api/matches/{id}/rounds` | GET | List round summaries |
| `/api/matches/{id}/rounds/{num}` | GET | Full round detail |
| `/api/matches/{id}/pause` | POST | Pause match |
| `/api/matches/{id}/resume` | POST | Resume match |
| `/api/matches/{id}/cancel` | POST | Cancel match |
| `/api/models` | GET | List available LLMs |
| `/api/leaderboard` | GET | Aggregated statistics |
| `/api/health` | GET | System health check |
| `/api/debug/generate-state` | POST | Generate game state |
| `/api/debug/generate-sentences` | POST | Generate engine sentences |
| `/api/debug/test-prompt` | POST | Test raw LLM prompt |

## Roadmap

- [x] Core Game Engine (State generator, 13 sentence templates, scoring)
- [x] Ollama LLM Adapter with retry logic
- [x] Match Orchestrator (Background execution, pause/resume/cancel)
- [x] Backend REST API (14 endpoints, Swagger)
- [x] EF Core + SQLite persistence
- [x] Web UI — Dashboard, Match management, Round detail
- [x] Leaderboard and Analysis pages with Chart.js
- [x] Debug tools page
- [x] SignalR real-time match progress
- [x] Docker infrastructure (Ollama GPU + API + WebUI)
- [ ] Extended integration tests
- [ ] CI/CD pipeline hardening

## Key Design Principles

1. **Zero Luck** — No randomness in scoring. All outcomes are deterministic based on LLM decisions.
2. **Procedural Generation** — Infinite unique game states. No memorization possible.
3. **Fairness** — All LLMs receive identical prompts per round. No model-specific tuning.
4. **Reproducibility** — Every match is seeded. Results are fully reproducible.
5. **Scalability** — Computationally cheap. Can run thousands of matches per hour.

## License

MIT
