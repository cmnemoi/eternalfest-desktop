# Offline backend

## Why

The Eternalfest loader expects an Eternalfest server on the origin it was loaded from. The launcher must pretend to be that server, locally and invisibly, so a cached contrée runs with full options and without eternalfest.net.

## Scope

The HTTP contract the embedded backend serves to the loader during one game session. The routes and response shapes follow what the loader requests, as recorded in `hammerfest-tas/mirror/`.

## Required constraints

- It listens on `127.0.0.1` only, on a free port chosen when it starts (see ADR 0003).
- The loader, the engine, the patcher and the API share that single origin.
- It never contacts eternalfest.net.

## Rules

### Serves the bundled loader and base engine

`{#backend::serves-bundled-assets}`

`GET /assets/loader.swf` returns the bundled loader, and `GET /assets/game.swf` the bundled base engine, both as `application/x-shockwave-flash`.

### Serves cached blobs

`{#backend::serves-cached-blobs}`

`GET /api/v1/blobs/{id}/raw` returns the cached blob with its original media type. An unknown blob id returns 404.

### Serves game details with full options

`{#backend::serves-game-full-options}`

`GET /api/v1/games/{id}` returns the cached details of the downloaded build, unlocked for the chosen player profile (see the player-profile spec): its families, and the visibility of its modes and options. Every visible option is enabled. Options still hidden (`is_visible: false`) stay as published. An unknown game returns 404.

### Starts a run with the player's families and inventory

`{#backend::starts-run-player-inventory}`

`POST /api/v1/runs/{id}/start` for the current run returns the run reference, a key, the families of the build unlocked for the player, and the player's inventory as item id to quantity.

### Accepts results and reports the game end

`{#backend::discards-results}`

`POST /api/v1/runs/{id}/result` accepts the result, returns the run with that result attached, then reports the game end with that result (see `play::closes-on-game-end`). Nothing is persisted or sent anywhere.

### Rejects anything else

`{#backend::rejects-unknown-routes}`

Any other request returns 404 and is logged with its method and path, so missing routes show up in the logs.

## Acceptance criteria

- Given the backend is started, when the loader is requested, then the bundled loader bytes are returned.
- Given a cached blob, when it is requested, then its bytes and media type are returned. Given an unknown blob, then 404.
- Given a build with an option `is_visible: true, is_enabled: false`, when game details are requested, then that option is enabled.
- Given a build with an option `is_visible: false` that no quest unlocks, when game details are requested, then it is unchanged.
- Given *Les Cavernes de Hammerfest* played with the complete profile, when game details are requested, then *Intuition* is visible and enabled, and the families are the unlocked ones.
- Given a build with families `"0,1,2,1000"` played as a new player, when the run is started, then the families are `"0,1,2,1000"` and items is empty.
- Given *Les Cavernes de Hammerfest* played with the complete profile, when the run is started, then the families are the unlocked ones and items holds 9999 of every item.
- Given a run started for game A, when a start is requested for another run id, then 404.
- Given a finished game, when the result is posted, then the answer echoes the run with its result, the game end is reported with that result, and nothing is written to disk.
- Given the recorded loader requests of `hammerfest-tas/mirror/`, when they are replayed against the backend, then every response has the same shape as the recording.
- Given two backends started one after the other, then each gets a free port and the second doesn't fail because of the first.

## Out of scope

- `GET /api/v1/auth/self`, leaderboards, profiles, users (the loader doesn't use them).
- Several simultaneous game sessions.
- Serving over HTTPS.
