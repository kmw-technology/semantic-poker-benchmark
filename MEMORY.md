# Semantic Poker Benchmark - Memory File

**WICHTIG: Diese Datei enthält kritische Informationen, die Claude bei JEDER Conversation lesen sollte. Sie fungiert als "Langzeitgedächtnis".**

---

## CONTEXT-VERLUST? LIES DAS HIER!

**Wenn du (Claude) unsicher bist oder dich nicht erinnerst:**
1. Diese ganze Datei lesen
2. CLAUDE.md Status-Block prüfen
3. `git log --oneline -20` für letzte Änderungen
4. Bei Unklarheit: User fragen!

**Symptome von Context-Verlust:**
- Du fragst etwas, das schon entschieden wurde
- Du weißt nicht mehr, was die aktuelle Aufgabe ist
- Du schlägst etwas vor, das User-Präferenzen widerspricht

→ **Lösung:** Diese Datei komplett neu lesen!

---

## Aktuelle Projekt-Phase

**Phase:** Foundation
**Status:** Repository erstellt, Blueprint angepasst, Projektstruktur definiert
**Nächste Schritte:** Core Game Engine implementieren (State Generator, Sentence Engine, Scoring)

---

## PROJEKT-ESSENZ (Die 5 wichtigsten Punkte)

1. **Zero-Luck Benchmark** — Kein Zufall, 100% deterministisch, testet reine kognitive Fähigkeiten von LLMs
2. **Theory of Mind** — Testet die Fähigkeit von LLMs, den Gegner psychologisch zu modellieren (Level-k Thinking)
3. **The Oracle's Bluff** — Formalisiertes Bluffing-Spiel: Architect täuscht, Players deduzieren
4. **Fairness** — Alle LLMs bekommen identische Prompts, Engine-Sätze sind vom Architect nicht unterscheidbar
5. **Skalierbar** — Prozedural generierte States, unendlich viele Runden möglich

---

## GAME DESIGN: "The Oracle's Bluff"

### Setup (Hidden State)
- Engine generiert 5 Doors (A-E)
- 1 Door = Treasure (Player gewinnt), 1 Door = Trap (Architect gewinnt), 3 = Empty (neutral)

### Phase 1: Engine's Truth
- Engine generiert exakt 2 wahre Sätze basierend auf dem Hidden State

### Phase 2: Architect's Deception
- Architect (LLM 1) sieht den vollen State + die 2 Engine-Sätze
- Architect schreibt bis zu 3 eigene Sätze (Wahrheit, Halbwahrheit, oder Lüge)
- Alle 5 Sätze werden gemischt — Herkunft ist für Players unsichtbar

### Phase 3: Player Deduction
- 3 Player-LLMs erhalten die 5 gemischten Sätze
- Jeder Player wählt unabhängig eine Door
- Scoring: Treasure = Player +1, Trap = Architect +1, Empty = 0

### Was wird getestet:
1. **Logical Contradiction Detection** — Widersprüche zwischen Sätzen erkennen
2. **Linguistic Anomaly Detection** — Engine-Syntax vs. Architect-Stil unterscheiden
3. **Level-k Thinking** — Rekursives "Ich weiß, dass du weißt, dass ich weiß..."

---

## KRITISCHE DATEIEN (PFLICHTLEKTÜRE VOR AKTIONEN!)

| Aktion | ZUERST LESEN |
|--------|--------------|
| **Code schreiben** | `.claude/markdown/CODE-QUALITY.md` (YAGNI/KISS) |
| **Tests schreiben** | `.claude/markdown/TESTING-STRATEGY.md` |
| **UI ändern** | `.claude/markdown/DESIGN-SYSTEM.md` |
| **Bei Fehler** | `LESSONS-LEARNED.md` → Fehler dokumentieren! |

---

## Kritische Entscheidungen (Zusammenfassung)

| Entscheidung | Begründung | ADR |
|--------------|------------|-----|
| C# / ASP.NET Core 8 | User-Präferenz (CLAUDE.md global), robustes Typsystem für Game Engine | ADR-0001 |
| SQLite für lokale Results | Einfach, kein Setup nötig, reicht für Benchmark-Daten | ADR-0002 |

**Vollständige ADRs:** `.claude/markdown/adr/`

---

## User-Präferenzen

| Bereich | Präferenz | Datum |
|---------|-----------|-------|
| Memory-System | Claude soll IMMER und UNGEFRAGT wichtige Dinge dokumentieren | 2026-02-25 |
| Autonomie | Autonom arbeiten, aber KRITISCH gegenüber User-Prompts — bei Problemen/Unklarheiten NACHFRAGEN! | 2026-02-25 |
| Tech-Stack | ASP.NET Core C# Razor Pages bevorzugt (global CLAUDE.md) | 2026-02-25 |
| API-First | Alles zuerst als API, UI konsumiert nur über HTTP | 2026-02-25 |

---

## Offene Fragen (zu klären)

| Frage | Kontext | Status |
|-------|---------|--------|
| Welche LLM-Provider sollen initial unterstützt werden? | OpenAI, Anthropic, Google? | Offen |
| Wie viele Runden pro Match? | Statische Config oder dynamisch? | Offen |
| Soll es ein Web-UI geben oder CLI-only? | Razor Pages vs. Console App | Offen |
| Scoring-System: ELO oder einfache Punkte? | Turnier-Format definieren | Offen |

---

## Aktive Warnungen & Bekannte Probleme

| Problem | Workaround | Ticket |
|---------|------------|--------|
| - | - | - |

---

## Deployment-Informationen

### Development
```bash
# App starten
dotnet run --project projects/orchestrator/src/

# Tests ausführen
dotnet test
```

---

## Wichtige Kontakte & Ressourcen

- **Repository:** https://github.com/kmw-technology/semantic-poker-benchmark
- **CI/CD:** GitHub Actions

---

## Modul-Status

| Modul | Status | Version | Notizen |
|-------|--------|---------|---------|
| game-engine | Geplant | - | Core Game Logic: State, Sentences, Scoring |
| orchestrator | Geplant | - | LLM API Integration, Match Runner |
| shared | Geplant | - | DTOs, Interfaces, Enums |

---

## Code-Qualitäts-Metriken (Ziel)

| Metrik | Ziel | Aktuell |
|--------|------|---------|
| Test Coverage | >70% | - |
| Build Zeit | <5min | - |
| Technical Debt | <2h | - |

---

## Langzeit-Strategien

### Testing
- **Strategie:** Test-Pyramide (70% Unit, 20% Integration, 10% E2E)
- **Coverage-Gate:** Build scheitert unter 70%
- **Docs:** `.claude/markdown/TESTING-STRATEGY.md`

---

## Letzte wichtige Änderungen

| Datum | Änderung | Autor |
|-------|----------|-------|
| 2026-02-25 | Projekt initialisiert mit Blueprint | Claude |

---

## Session-Log (Gegen Context-Verlust)

| Session-Datum | Zusammenfassung | Ergebnis |
|---------------|-----------------|----------|
| 2026-02-25 | Repository erstellt, Blueprint angepasst, README geschrieben | Setup Complete |

---

## Learnings (Was wir gelernt haben)

| Datum | Learning | Kontext |
|-------|----------|---------|
| 2026-02-25 | Memory-Updates müssen SOFORT und UNGEFRAGT passieren | Claude vergisst sonst wichtige Dinge |
| 2026-02-25 | Autonom ≠ Blind ausführen | Bei Unklarheiten/Problemen IMMER nachfragen, nicht raten |
