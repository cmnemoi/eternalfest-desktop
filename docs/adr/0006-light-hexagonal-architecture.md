# 0006 — Light hexagonal architecture and naming conventions

- Status: accepted
- Date: 2026-09-24

## Context

The app has four boundaries that must be replaceable in tests: the eternalfest.net API, the file system cache, the embedded HTTP backend and the Ruffle process. The UI must stay thin.

## Decision

Projects (names may still be refined):

| Project | Contains | Depends on |
|---|---|---|
| `EternalfestDesktop.Domain` | Contrées, builds, modes, options, runs, and the full options rules | nothing |
| `EternalfestDesktop.Application` | Use cases (browse the catalog, download a contrée, check for updates, play a contrée) and ports | Domain |
| `EternalfestDesktop.Infrastructure` | Adapters: the eternalfest.net API client, the file system store, the Kestrel backend, the Ruffle process | Application |
| `EternalfestDesktop.Ui` | Avalonia views and view models, composition root | Application, Infrastructure |
| `EternalfestDesktop.Cli` | Console entry point used for the walking skeleton and debugging | Application, Infrastructure |
| `*.Tests` | Unit, acceptance (DSL) and contract tests | — |

Conventions:

- **No `I` prefix on interfaces.** A port is named after its role (`GameCatalog`, `GameStore`, `OfflineBackend`, `FlashPlayer`), and an adapter after its technology (`EternalfestApiGameCatalog`, `FileSystemGameStore`, `KestrelOfflineBackend`, `RuffleFlashPlayer`).
- Code, docs and commits are in English.
- Behaviors carry stable spec IDs (`{#area::behavior}`), referenced from tests and code with `@spec`.
- TDD with xUnit. Acceptance tests speak the business DSL and run against in-memory doubles of the ports. Adapters get their own contract tests.

## Consequences

- Each risky boundary can be tested alone, and use cases can be tested without network, disk or Ruffle.
- There are more projects than a single Avalonia app would need, which is acceptable for the testability it buys.
