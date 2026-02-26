# Semantic Poker Benchmark - Memory File

**WICHTIG: Diese Datei enthält kritische Informationen, die Claude bei JEDER Conversation lesen sollte.**

---

## CONTEXT-VERLUST? LIES DAS HIER!

1. Diese ganze Datei lesen
2. CLAUDE.md Status-Block prüfen
3. `git log --oneline -20` für letzte Änderungen
4. Bei Unklarheit: User fragen!

---

## Aktuelle Projekt-Phase

**Phase:** Implementation Complete (Phase 1-6 fertig)
**Status:** Alle Kernfunktionalität implementiert, Build + 189 Tests bestanden
**Nächste Schritte:** Docker-Deployment testen, echte Matches mit Ollama laufen lassen

---

## PROJEKT-ESSENZ

1. **Zero-Luck Benchmark** — Kein Zufall, 100% deterministisch, testet reine kognitive Fähigkeiten von LLMs
2. **Theory of Mind** — Testet Level-k Thinking: "Ich weiß, dass du weißt, dass ich weiß..."
3. **The Oracle's Bluff** — Formalisiertes Bluffing-Spiel: Architect täuscht, Players deduzieren
4. **Fairness** — Alle LLMs bekommen identische Prompts pro Runde
5. **Skalierbar** — Prozedural generierte States, unendlich viele Runden möglich

---

## ARCHITEKTUR

### Solution (9 Projekte)

| Projekt | Pfad | Referenzen | Funktion |
|---------|------|------------|----------|
| **SemanticPoker.Shared** | `projects/shared/src/` | KEINE | Enums, Models, Interfaces, DTOs |
| **SemanticPoker.GameEngine** | `projects/game-engine/src/` | → Shared | StateGenerator, 13 SentenceTemplates, ScoreCalculator |
| **SemanticPoker.Api** | `projects/orchestrator/src/` | → Shared, GameEngine | REST API, EF Core, Ollama, Match-Orchestrierung |
| **SemanticPoker.WebUI** | `projects/webui/src/` | → Shared **NUR** | Razor Pages, BenchmarkApiClient via HTTP |
| 5 Test-Projekte | `*/tests/` | xUnit | 189 Tests |

### MODUL-ABHÄNGIGKEITS-REGELN (KRITISCH!)
```
ERLAUBT:
  GameEngine → Shared
  Api → Shared + GameEngine
  WebUI → Shared (NUR! Nie GameEngine oder Api!)

VERBOTEN:
  GameEngine → Api (Engine kennt keine LLMs!)
  Shared → irgendwas
  WebUI → GameEngine/Api (UI nur über HTTP!)
```

### Ports & Services

| Service | Port | Swagger |
|---------|------|---------|
| Backend API | 5000 | http://localhost:5000/swagger |
| Web UI | 5010 | - |
| Ollama | 11434 | - |
| SignalR Hub | 5000/hubs/match-progress | - |

### API Endpoints (14)

| Route | Method | Controller |
|-------|--------|------------|
| `/api/matches` | GET, POST | MatchesController |
| `/api/matches/{id}` | GET | MatchesController |
| `/api/matches/{id}/rounds` | GET | MatchesController |
| `/api/matches/{id}/rounds/{num}` | GET | MatchesController |
| `/api/matches/{id}/pause` | POST | MatchesController |
| `/api/matches/{id}/resume` | POST | MatchesController |
| `/api/matches/{id}/cancel` | POST | MatchesController |
| `/api/models` | GET | ModelsController |
| `/api/leaderboard` | GET | LeaderboardController |
| `/api/health` | GET | HealthController |
| `/api/debug/generate-state` | POST | DebugController |
| `/api/debug/generate-sentences` | POST | DebugController |
| `/api/debug/test-prompt` | POST | DebugController |

### Game Engine (13 Sentence Templates)

| ID | Template | Beschreibung |
|----|----------|-------------|
| negative-door | NegativeDoorTemplate | "X ist NICHT hinter Door Y" |
| pair-exclusion | PairExclusionTemplate | "X ist weder hinter Y noch Z" |
| triplet-contains | TripletContainsTemplate | "X ist in {Y, Z, W}" |
| positional-relation | PositionalRelationTemplate | "X ist N Doors von Y entfernt" |
| adjacent-door | AdjacentDoorTemplate | "Door neben X ist Y" |
| same-side | SameSideTemplate | "X und Y auf gleicher Seite" |
| gap | GapTemplate | "N Empty Doors zwischen X und Y" |
| endpoint | EndpointTemplate | "X ist am Rand" |
| middle-door | MiddleDoorTemplate | "X ist (nicht) Door C" |
| empty-neighbor | EmptyNeighborTemplate | "Door X hat N leere Nachbarn" |
| order | OrderTemplate | "X kommt vor/nach Y" |
| exact-empty | ExactEmptyTemplate | "Door X ist Empty" |
| qualitative-distance | QualitativeDistanceTemplate | "X und Y sind nah/mittel/weit" |

---

## Kritische Entscheidungen

| Entscheidung | Begründung |
|--------------|------------|
| C# / ASP.NET Core 8 | User-Präferenz, starkes Typsystem |
| SQLite via EF Core | Einfach, kein Setup, reicht für Benchmarks |
| Ollama für LLM-Serving | Docker, GPU, OpenAI-kompatible API |
| Template-basierte Sentences | 13 rigide Templates, deterministisch |
| Round-Robin Architect | `(roundNumber - 1) % modelCount` |
| Adaptive Play | Letzte 5 Runden als Kontext |
| Channel-based Queue | Background-Service für Match-Execution |
| SignalR | Echtzeit-Updates für Match-Fortschritt |

---

## User-Präferenzen

| Bereich | Präferenz |
|---------|-----------|
| Tech-Stack | ASP.NET Core C# Razor Pages |
| API-First | UI konsumiert nur über HTTP |
| Autonomie | Autonom aber kritisch — bei Problemen nachfragen |
| Dokumentation | IMMER und UNGEFRAGT wichtige Dinge dokumentieren |
| Sprache | Mix Deutsch/Englisch OK, Code immer Englisch |

---

## Deployment

### Docker (Empfohlen)
```bash
cp deployment/.env.example deployment/.env
docker compose -f deployment/docker-compose.yml up -d
```

### Lokal
```bash
dotnet build                                           # Build
dotnet test                                            # 189 Tests
dotnet run --project projects/orchestrator/src/        # API auf :5000
dotnet run --project projects/webui/src/               # UI auf :5010
```

### LLM-Modelle (Ollama)
- `phi3.5` — Phi-3.5-mini-instruct
- `deepseek-r1:1.5b` — DeepSeek-R1-Distill-Qwen-1.5B
- `llama3.2:3b` — Llama-3.2-3B

---

## Implementation Status

| Phase | Beschreibung | Status | Commit |
|-------|-------------|--------|--------|
| Phase 1 | Shared + Game Engine (185 Tests) | DONE | c12b9c8 |
| Phase 2 | Backend REST API (14 Endpoints) | DONE | deb49fd |
| Phase 3 | Docker Infrastructure | DONE | e55c3fd |
| Phase 4 | Web UI (7 Seiten) | DONE | c720cb8 |
| Phase 5 | Debug Page + SignalR | DONE | 117b193 |
| Phase 6 | CI Pipeline + README | DONE | 47d2035 |

---

## Aktive Warnungen

| Problem | Details |
|---------|---------|
| Placeholder Tests | Api.Tests, WebUI.Tests, E2E.Tests, Shared.Tests haben nur je 1 Placeholder-Test |
| Kein Polly Retry | HttpClient für WebUI→API hat kein Polly konfiguriert (Package vorhanden, aber nicht genutzt) |

---

## Kontakte & Ressourcen

- **Repository:** https://github.com/kmw-technology/semantic-poker-benchmark
- **Git Identity:** kmw-technology / kmw-technology@users.noreply.github.com
- **CI/CD:** GitHub Actions (`.github/workflows/ci.yml`)
