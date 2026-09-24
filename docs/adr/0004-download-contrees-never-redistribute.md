# 0004 — Download contrées from the public API, never redistribute them

- Status: accepted
- Date: 2026-09-24

## Context

Offline play needs each contrée's build artifacts: the engine SWF, the patcher SWF, the level XML, translations, musics and the icon. The options were:

- Building them locally: needs Haxe, swfmill and a private npm registry. Excluded.
- Bundling them in the installer: contrées declare `"license": "UNLICENSED"`, have no LICENSE file, and some are `"private": true`. No redistribution right is written anywhere.
- Fetching them from the public eternalfest.net API. Checked in the server sources (`eternalfest/eternalfest/crates`):
  - `GET /api/v1/games` and `GET /api/v1/games/:id` work anonymously and only expose games whose channel is publicly viewable.
  - `GET /api/v1/blobs/:id/raw` is public and served as `public,max-age=31536000,immutable`.
  - Game details list every blob with its size and SHA-256 digest.

## Decision

- The catalog is the live list of public games from the API, cached locally for offline browsing.
- A contrée is downloaded on the player's demand, once per build, into the local cache, and each blob is checked against its SHA-256 digest.
- Cached blobs are never downloaded again, since they are immutable.
- Contrées are never shipped in the app or its repository. Test fixtures only contain recorded API metadata, or small synthetic blobs.
- Requests identify the app with a descriptive `User-Agent`.

## Consequences

- Nothing to build locally and no licensing exposure for contrée content.
- The network is needed for the first download of each contrée, and never afterwards.
- Private or unlisted contrées aren't available, which is intended.
- The app depends on an undocumented API. Breaking changes affect downloads only.
