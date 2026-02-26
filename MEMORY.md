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

**Phase:** Implementation Complete (Phase 1-6 + Interactive Mode + Spectator + OpenAI)
**Status:** Alle Kernfunktionalität implementiert, Build + 189 Tests bestanden
**Nächste Schritte:** Docker-Deployment testen, echte Matches mit Ollama + OpenAI laufen lassen

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
| **SemanticPoker.Api** | `projects/orchestrator/src/` | → Shared, GameEngine | REST API, EF Core, LLM Adapters, Match-Orchestrierung |
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

### LLM Providers (CompositeAdapter Pattern)

Die `ILlmAdapter`-Implementierung ist ein `CompositeAdapter`, der Requests anhand des Model-ID-Prefix an den richtigen Sub-Adapter routet:

| Provider | Adapter | Model-ID-Prefix | Beispiel |
|----------|---------|-----------------|----------|
| **Ollama** | `OllamaAdapter` | (kein Prefix) | `phi3.5:latest`, `llama3.2:3b` |
| **OpenAI** | `OpenAiAdapter` | `openai:` | `openai:gpt-5-mini:low` |

**OpenAI Model-ID Format:** `openai:{model}:{reasoning_effort}` → z.B. `openai:gpt-5-mini:medium`

**Dateien:**
- `projects/orchestrator/src/Infrastructure/LlmAdapters/OllamaAdapter.cs`
- `projects/orchestrator/src/Infrastructure/LlmAdapters/OpenAiAdapter.cs`
- `projects/orchestrator/src/Infrastructure/LlmAdapters/CompositeAdapter.cs`

### API Endpoints (17)

| Route | Method | Controller |
|-------|--------|------------|
| `/api/matches` | GET, POST | MatchesController |
| `/api/matches/{id}` | GET | MatchesController |
| `/api/matches/{id}/rounds` | GET | MatchesController |
| `/api/matches/{id}/rounds/{num}` | GET | MatchesController |
| `/api/matches/{id}/pause` | POST | MatchesController |
| `/api/matches/{id}/resume` | POST | MatchesController |
| `/api/matches/{id}/cancel` | POST | MatchesController |
| `/api/matches/interactive` | POST | MatchesController |
| `/api/matches/{id}/interactive-state` | GET | MatchesController |
| `/api/matches/{id}/human-input?playerId=` | POST | MatchesController |
| `/api/models` | GET | ModelsController |
| `/api/leaderboard` | GET | LeaderboardController |
| `/api/health` | GET | HealthController |
| `/api/debug/generate-state` | POST | DebugController |
| `/api/debug/generate-sentences` | POST | DebugController |
| `/api/debug/test-prompt` | POST | DebugController |
| `/api/debug/matches/{id}/trace` | GET | DebugController |
| `/api/debug/matches/{id}/llm-stats` | GET | DebugController |
| `/api/debug/matches/{id}/raw-responses` | GET | DebugController |
| `/api/debug/matches/{id}/rounds/{num}/prompts` | GET | DebugController |
| `/api/debug/parse-failures` | GET | DebugController |

### Web UI Pages (11)

| Seite | Pfad | Funktion |
|-------|------|----------|
| Dashboard | `/` | Übersicht, aktive Matches, Statistiken |
| New Match | `/Matches/New` | Neues automatisches Match starten |
| Match Detail | `/Matches/Detail/{id}` | Match-Details, Runden-Ergebnisse |
| Round Detail | `/Matches/Detail/Round/{id}/{num}` | Einzel-Runden-Analyse |
| Leaderboard | `/Leaderboard` | Rangliste aller Modelle |
| Analysis | `/Analysis` | Erweiterte Analyse-Diagramme |
| Debug | `/Debug` | Debug-Tools für Entwickler |
| Lobby Create | `/Lobby/Create` | Interaktives Match erstellen (Spieler-Slots) |
| Lobby Play | `/Lobby/Play/{id}` | Als Mensch mitspielen (Echtzeit) |
| Spectate Index | `/Spectate` | Liste aller Live-Spiele |
| Spectate Watch | `/Spectate/Watch/{id}` | Match live zuschauen (Read-Only) |

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
| Ollama + OpenAI (dual provider) | Lokale + Cloud-Modelle im selben Benchmark vergleichbar |
| CompositeAdapter Pattern | Routet "openai:*" → OpenAI, sonst → Ollama. Zero Änderungen an Consumern |
| Template-basierte Sentences | 13 rigide Templates, deterministisch |
| Round-Robin Architect | `(roundNumber - 1) % modelCount` |
| Adaptive Play | Letzte 5 Runden als Kontext |
| Channel-based Queue | Background-Service für Match-Execution |
| SignalR | Echtzeit-Updates für Match-Fortschritt |
| PlayerSlot-basierte Lobby | Dynamisch Human/LLM-Slots, min. 3 Spieler, max. 8 |
| HumanInputCoordinator | TCS-basierte Brücke: HTTP-Endpoint ↔ Game-Loop (composite key: matchId + playerId) |
| Human-Player Convention | `"human:{Name}"` als ModelId, `IsHumanPlayer(id) => id.StartsWith("human:")` |
| OpenAI Reasoning Effort | Im Model-ID codiert: `openai:gpt-5-mini:low` → model=gpt-5-mini, reasoning_effort=low |

---

## User-Präferenzen

| Bereich | Präferenz |
|---------|-----------|
| Tech-Stack | ASP.NET Core C# Razor Pages |
| API-First | UI konsumiert nur über HTTP |
| **Runtime** | **IMMER Docker-Container verwenden! NIEMALS lokale `dotnet run` Instanzen starten!** |
| Autonomie | Autonom aber kritisch — bei Problemen nachfragen |
| Dokumentation | IMMER und UNGEFRAGT wichtige Dinge dokumentieren |
| Sprache | Mix Deutsch/Englisch OK, Code immer Englisch |

### DOCKER-FIRST (KRITISCH!)
- API und WebUI laufen **ausschließlich als Docker-Container** (`docker compose -f deployment/docker-compose.yml up -d`)
- **NIEMALS** `dotnet run --project projects/orchestrator/src/` oder `dotnet run --project projects/webui/src/` verwenden!
- Lokale `dotnet run` Instanzen verursachen Port-Konflikte (binden auf `localhost:5000` vor Docker `0.0.0.0:5000`)
- Bei Port-Konflikten: `netstat -ano | grep ":5000"` prüfen, lokale Prozesse stoppen
- Für Code-Änderungen: `docker compose -f deployment/docker-compose.yml up -d --build` (neu bauen)

---

## Deployment

### Docker (EINZIGE Runtime-Methode!)
```bash
cp deployment/.env.example deployment/.env
# OpenAI API Key, Org-ID, Project-ID in .env eintragen
docker compose -f deployment/docker-compose.yml up -d          # Starten
docker compose -f deployment/docker-compose.yml up -d --build  # Nach Code-Änderungen
docker compose -f deployment/docker-compose.yml down            # Stoppen
```

### Nur für Build & Tests (NICHT zum Laufen!)
```bash
dotnet build                                           # Build prüfen
dotnet test                                            # 189 Tests
# NICHT: dotnet run! → Immer Docker verwenden!
```

### Umgebungsvariablen für OpenAI (lokal)
```bash
export OpenAi__ApiKey="sk-..."
export OpenAi__OrganizationId="org-..."
export OpenAi__ProjectId="proj_..."
```

### LLM-Modelle

**Ollama (lokal):**
- `phi3.5` — Phi-3.5-mini-instruct
- `deepseek-r1:1.5b` — DeepSeek-R1-Distill-Qwen-1.5B
- `llama3.2:3b` — Llama-3.2-3B

**OpenAI (Cloud, API Key erforderlich):**
- `openai:gpt-5-mini:low` — GPT-5 Mini, Low Reasoning
- `openai:gpt-5-mini:medium` — GPT-5 Mini, Medium Reasoning
- `openai:gpt-5-mini:high` — GPT-5 Mini, High Reasoning
- `openai:gpt-5-nano:low` — GPT-5 Nano, Low Reasoning
- `openai:gpt-5-nano:medium` — GPT-5 Nano, Medium Reasoning
- `openai:gpt-5-nano:high` — GPT-5 Nano, High Reasoning

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
| Interactive Lobby | Human+LLM PlayerSlots, HumanInputCoordinator | DONE | 4c89aa0 |
| Spectator Mode | Live Games Listing + Watch Page | DONE | - |
| OpenAI Integration | 6 Models, CompositeAdapter, Reasoning Effort | DONE | - |

---

## Aktive Warnungen

| Problem | Details |
|---------|---------|
| Placeholder Tests | Api.Tests, WebUI.Tests, E2E.Tests, Shared.Tests haben nur je 1 Placeholder-Test |
| Kein Polly Retry | HttpClient für WebUI→API hat kein Polly konfiguriert (Package vorhanden, aber nicht genutzt) |
| OpenAI Kosten | OpenAI-Modelle kosten Geld pro API-Call — Vorsicht bei vielen Runden! |
| Schema-Änderung | Nach Änderungen an MatchEntity muss `benchmark.db` gelöscht werden (EnsureCreatedAsync) |
| Docker Port-Konflikt | Wenn lokale `dotnet run` + Docker gleichzeitig laufen, bindet localhost auf lokale Instanz → WebUI sieht andere DB! |
| Docker GPU | `docker-compose.yml` hat KEINE NVIDIA GPU Reservation — System hat keine NVIDIA GPU |
| Docker Healthcheck | Ollama Healthcheck nutzt `ollama list` (nicht curl — curl fehlt im Image) |
| Dockerfile --no-restore | Entfernt! `dotnet publish` macht eigenen Restore, da transitive Pakete sonst fehlen |

---

## Bekannte Razor-Pitfalls

| Problem | Lösung |
|---------|--------|
| `@keyframes` in CSS-Block | `@@keyframes` verwenden (doppeltes @) |
| `@model` als Variable in foreach | `@(model)` verwenden (Klammern) |

---

## Kontakte & Ressourcen

- **Repository:** https://github.com/kmw-technology/semantic-poker-benchmark
- **Git Identity:** kmw-technology / kmw-technology@users.noreply.github.com
- **CI/CD:** GitHub Actions (`.github/workflows/ci.yml`)
