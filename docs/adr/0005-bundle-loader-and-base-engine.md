# 0005 — Bundle the loader and base engine

- Status: accepted
- Date: 2026-09-24

## Context

Besides contrée blobs, the loader needs:

- `/assets/loader.swf`: the Eternalfest loader, `@eternalfest/loader` 5.1.2, AGPL-3.0-or-later.
- `/assets/game.swf`: the base Hammerfest engine, `@eternalfest/game`, MIT. It is only used by builds whose engine is `V96`, since most contrées ship a `Custom` engine blob.

eternalfest.net serves both publicly. They could be downloaded at runtime like contrées, or bundled.

## Decision

- Bundle both files in the app, at pinned versions, together with their license notices (the AGPL-3.0 source offer for the loader points to its upstream repository).
- The offline backend serves them from the bundle.

## Consequences

- Ruffle and the loader are tested together, and a loader update on eternalfest.net can't break offline play unexpectedly.
- A contrée that requires a newer loader than the bundled one may fail to load. The build's `loader` field is known, so the launcher can warn about it (see the `play-game` spec).
- Upgrading the loader requires an app release.
