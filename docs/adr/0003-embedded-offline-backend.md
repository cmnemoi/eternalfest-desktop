# 0003 — Embedded Kestrel offline backend on a single local origin

- Status: accepted
- Date: 2026-09-24

## Context

The Eternalfest loader (`loader.swf`) makes real HTTP requests: `GET /api/v1/games/:id`, `GET /api/v1/blobs/:id/raw`, `POST /api/v1/runs/:id/start` and `POST /api/v1/runs/:id/result`. Facts from the loader sources (`eternalfest/loader`):

- The API origin is derived from the loader's own URL (`origin + "/api/v1"`). No hostname is hard-coded.
- The engine and patcher must share the loader's origin, port included, or it fails with `InvalidGameEngineOrigin`.
- Plain HTTP is accepted (`Security.allowInsecureDomain("*")`).
- The `localhost` hostname turns on the loader's debug console, while `127.0.0.1` doesn't.

`eternaldev` (Node, AGPL-3.0) is too heavy to ship and would need a Node runtime.

## Decision

- The launcher hosts a minimal ASP.NET Core Kestrel app in its own process.
- It listens on `http://127.0.0.1:<random free port>` and serves the loader, the base engine, the API and blobs on that single origin.
- It starts when a game is played and stops when Ruffle exits.
- It is written from scratch against the observed HTTP contract (the captures in `hammerfest-tas/mirror/`), not ported from `eternaldev`, so the project stays GPL-3.0.

## Consequences

- From the player's point of view there is no server: nothing to start and no terminal.
- Routes can be tested in memory with `TestServer`, replaying recorded loader requests.
- ASP.NET Core adds size to the self-contained archive. If that becomes a problem, it can be swapped for `HttpListener` behind the `OfflineBackend` port.
- Only loopback is bound, so the backend isn't reachable from the network.
