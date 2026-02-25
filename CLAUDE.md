# Semantic Poker Benchmark - Claude Code Anweisungen

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║  AKTUELLER STATUS (Letzte Aktualisierung: 2026-02-25)                       ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  Phase:      Foundation                                                      ║
║  Repository: https://github.com/kmw-technology/semantic-poker-benchmark       ║
║  Nächstes:   Core Game Engine implementieren                                 ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║  KRITISCH: Keine Production-Änderungen ohne Backup + Genehmigung!            ║
║  PFLICHT:  Lies MEMORY.md für vollständigen Kontext                          ║
║  DENKEN:   Lies .claude/markdown/CRITICAL-THINKING.md - Risk-Matrix!         ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

---

## DIE 10 GEBOTE (TL;DR)

```
1. VERSTEHEN vor HANDELN     → Bei Unklarheit: FRAGEN, nicht raten!
2. EINFACHSTE Lösung         → Over-Engineering ist verboten
3. KRITISCH sein             → "Ist das wirklich eine gute Idee?"
4. NACHFRAGEN bei Red Flags  → "verbessere", "mach mal", "schnell" → STOPP!
5. KEINE Annahmen            → Lieber einmal zu viel fragen
6. AUTO-COMMIT häufig        → Nach jedem Feature/Fix sofort committen
7. MEMORY.md aktualisieren   → Bei jeder Entscheidung/Präferenz
8. SCOPE einhalten           → Nur machen was gefragt wurde
9. SICHERHEIT geht vor       → API Keys nur über Env-Vars
10. DOKUMENTIEREN            → Sofort und ungefragt
```

**Diese 10 Regeln gelten bei JEDER Nachricht, auch nach 100+ Messages!**

**Risk-Matrix & Details:** `.claude/markdown/CRITICAL-THINKING.md`

---

## RISK-MATRIX (Kurzfassung)

| Kategorie | Aktion | Beispiele |
|-----------|--------|-----------|
| **act_now** | Autonom, kein Report | Typos, Imports, Formatierung |
| **act_and_report** | Autonom, kurz erwähnen | Tests, kleine Refactorings (<30 Zeilen), Docs |
| **ask_first** | VOR Ausführung fragen | Neue Features, API-Änderungen, neue Dependencies |
| **forbidden** | Genehmigung + Bestätigung | API Keys committen, Force-Push, Benchmark-Manipulation |

**Vollständig:** `.claude/markdown/CRITICAL-THINKING.md`

---

## AUTONOMIE MIT KRITISCHEM DENKEN

**Autonom arbeiten ≠ Blind ausführen!**

### VOR jeder Aufgabe fragen:
```
□ Ist der Auftrag klar und eindeutig?
□ Gibt es potenzielle Probleme/Risiken?
□ Widerspricht es bestehenden Entscheidungen?
□ Fehlen mir wichtige Informationen?
□ Könnte das unbeabsichtigte Folgen haben?

→ Bei JA zu irgendeiner Frage: NACHFRAGEN!
```

### Proaktivität vs. Scope:

**Proaktiv SEIN bei:** Warnen, Quality-Fixes in bearbeiteten Dateien, Risiken ansprechen.
**Scope EINHALTEN bei:** Keine neuen Features, keine Architektur-Änderungen, keine Dateien außerhalb des Tasks.

**Kurzformel:** *Warnen und in bearbeiteten Dateien Quality-Fixes machen: ja. Neues anfangen: nein.*

---

## SICHERHEITS-REGELN (IMMER!)

### VERBOTEN ohne explizite Genehmigung:
- API Keys oder Secrets im Code
- Force-Push auf main
- Benchmark-Ergebnisse manipulieren
- LLM API-Calls ohne Kostenschätzung

### Bei gefährlichen Operationen → STOPPEN:
```
STOPP! Vor dieser Operation:
1. Welche Kosten entstehen? (LLM API-Calls)
2. Sind API Keys sicher? (nur .env)
3. Wird die Benchmark-Integrität gewahrt?
→ Ohne Bestätigung: NICHT AUSFÜHREN!
```

**Details:** `.claude/markdown/OPERATIONS-SECURITY.md`

---

## DOMAIN GUARDS (Projekt-spezifische Schutzregeln)

| Guard | Regel |
|-------|-------|
| **Benchmark-Integrität** | Ergebnisse nie manuell editieren, nur durch Engine generieren |
| **API Keys** | Nie in Code. Nie committen. Nur über .env und Umgebungsvariablen |
| **Fairness** | Alle LLMs erhalten identische Prompts pro Runde, kein Model-spezifisches Tuning |

---

## MEMORY-SYSTEM

### Bei Session-Start:
1. CLAUDE.md lesen (diese Datei) — Status-Block oben beachten!
2. MEMORY.md lesen — vollständiger Kontext
3. Bei Unklarheiten: `.claude/markdown/INDEX.md` prüfen

### Bei Unsicherheit / Verwirrung:
```
CONTEXT-REFRESH NÖTIG?
Wenn du unsicher bist was entschieden wurde:
→ MEMORY.md SOFORT komplett neu lesen!
→ git log --oneline -20 prüfen
→ User fragen wenn nötig
```

### Dokumenten-Hierarchie (bei Widersprüchen):
```
1. User-Anweisung (aktuell)     ← Höchste Priorität
2. MEMORY.md "User-Präferenzen" ← Persistierte User-Wünsche
3. CLAUDE.md                    ← Allgemeine Regeln
4. .claude/markdown/*.md        ← Detail-Docs
```
**Bei Konflikt: Höhere Ebene gewinnt!**

### SOFORT MEMORY.md aktualisieren bei:
| Event | Aktion |
|-------|--------|
| Architektur-Entscheidung | → MEMORY.md + neues ADR |
| User-Präferenz | → MEMORY.md "User-Präferenzen" |
| Bug/Workaround | → MEMORY.md "Aktive Warnungen" |
| Struktur-Änderung | → MEMORY.md + FOLDER-STRUCTURE.md |

---

## PROJEKT-STRUKTUR

```
semantic-poker-benchmark/
├── .claude/                        # Claude Code Konfiguration
│   ├── commands/                   # Custom Commands (cc-*)
│   └── markdown/                   # AI-Instruktionen & Docs
│       └── adr/                    # Architektur-Entscheidungen
├── projects/                       # Source Code aller Module
│   ├── game-engine/                # Core Game Engine (State, Sentences, Scoring)
│   ├── orchestrator/               # LLM API Orchestration & Match Runner
│   └── shared/                     # Gemeinsamer Code (Models, Interfaces)
├── tests/                          # Cross-Projekt Tests (E2E, Integration)
├── deployment/                     # Docker, CI/CD Configs
├── documentation/                  # Referenz-Dokumentation
├── tools/                          # Hilfs-Tools, Utilities
├── scripts/                        # Build-/Run-Scripts
├── resources/                      # Seed-Daten, Prompt-Templates
├── artifacts/                      # Build-Outputs, Benchmark-Reports
├── temporary/                      # Scratch-Dateien (gitignored)
├── secrets/                        # Lokale Secrets (gitignored)
├── CLAUDE.md                       # Diese Datei
├── MEMORY.md                       # Langzeit-Gedächtnis
└── LESSONS-LEARNED.md              # Persistentes Error-Learning
```

**Details:** `.claude/markdown/FOLDER-STRUCTURE.md`

---

## TECHNOLOGIE

| Komponente | Technologie |
|------------|-------------|
| Backend | ASP.NET Core 8 (C#) |
| API | REST (Minimal API oder Controllers) |
| Datenbank | SQLite (lokal) / PostgreSQL (optional) |
| LLM Integration | HttpClient + Provider-Adapters |
| CI/CD | GitHub Actions |
| Container | Docker (optional) |

---

## ARCHITEKTUR-PRINZIPIEN

### 1. API-First
- JEDE Funktionalität = zuerst API-Endpoint
- UI greift NUR über HTTP auf API zu
- NIEMALS direkter DB-Zugriff aus UI

### 2. Modulare Architektur
- **game-engine**: Generiert States, True Sentences, validiert und scored
- **orchestrator**: Steuert LLM-Calls, Match-Ablauf, Ergebnis-Aggregation
- **shared**: DTOs, Interfaces, Enums

### MODUL-ABHÄNGIGKEITS-REGELN (KRITISCH!)
```
ERLAUBT:
  game-engine → shared
  orchestrator → shared
  orchestrator → game-engine (über Interfaces)

VERBOTEN:
  game-engine → orchestrator (Engine kennt keine LLMs!)
  shared → game-engine/orchestrator
```

### 3. Clean Architecture
```
Domain (Models) → Application (Services) → Infrastructure (LLM Adapters, DB) → API
```

---

## CODING-STANDARDS (Kurzfassung)

- **Namenskonvention:** PascalCase für Klassen, _camelCase für private Felder
- **Max. Dateigröße:** 1000 Zeilen
- **Async:** Alle I/O-Operationen (LLM API Calls!)
- **Tests:** VOR jedem Commit, min. 70% Coverage
- **Logging:** Strukturiertes Logging für wichtige Operationen

**Vollständig:** `.claude/markdown/CODE-QUALITY.md`

---

## WICHTIGE BEFEHLE

```bash
# Build
dotnet build

# Tests
dotnet test

# App starten
dotnet run --project projects/orchestrator/src/

# Einzelnes Benchmark-Match starten (TODO)
dotnet run -- --match single --models gpt-4,claude-3.5,gemini-1.5
```

---

## DOKUMENTATION

### AI-Instruktionen (`.claude/markdown/`)

| Datei | Inhalt |
|-------|--------|
| `INDEX.md` | Welches Dokument wann lesen |
| `CRITICAL-THINKING.md` | Risk-Matrix, Red Flags, Pflicht-Pause |
| `CODE-QUALITY.md` | YAGNI, KISS, DRY, Code Smells |
| `TESTING-STRATEGY.md` | Test-Pyramide, Coverage |
| `DEPLOYMENT-RUNBOOK.md` | Deployment-Anleitung |
| `OPERATIONS-SECURITY.md` | Sicherheitsregeln, Umgebungen |
| `FOLDER-STRUCTURE.md` | Ordnerstruktur, 6-Ordner-Regel |
| `DESIGN-SYSTEM.md` | UI/UX Tokens und Regeln |
| `DOCKER-WORKFLOW.md` | Docker-First Development |
| `adr/` | Architektur-Entscheidungen |

### Root-Dateien

| Datei | Inhalt |
|-------|--------|
| `MEMORY.md` | Aktueller Status, Entscheidungen, Präferenzen |
| `LESSONS-LEARNED.md` | Fehler und Learnings |

---

## WIEDERHOLUNG: DIE 5 WICHTIGSTEN REGELN

1. **FRAGEN statt RATEN** — Bei Unklarheit IMMER nachfragen
2. **Risk-Matrix nutzen** — act_now / act_and_report / ask_first / forbidden
3. **MEMORY.md pflegen** — Jede Entscheidung dokumentieren
4. **AUTO-COMMIT** — Häufig und sofort
5. **SCOPE einhalten** — Nur machen was gefragt wurde

> **Mantra: "Habe ich das WIRKLICH verstanden oder nehme ich etwas an?"**
