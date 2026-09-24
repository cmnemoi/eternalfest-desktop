# Game catalog

## Why

Players need to see which contrées they can play, including when they are offline.

## Scope

Listing the public contrées and reading one contrée's details from eternalfest.net, and keeping a local copy of the catalog for offline browsing.

## Rules

### Lists every public contrée

`{#catalog::lists-public-contrees}`

The catalog contains every contrée that eternalfest.net lists to anonymous visitors, across all pages of the listing. Each entry has an identifier, a display name, a description and an icon.

### Reads the active build of a contrée

`{#catalog::reads-active-build}`

A contrée's details come from its active build: version, required loader version, engine (custom blob or base engine `V96`), patcher, content, content translations, musics, icon, modes with their options, and item families.

### Is read-only and anonymous

`{#catalog::read-only-anonymous}`

The catalog never sends credentials and never issues a write request to eternalfest.net. Every request carries the app's descriptive `User-Agent`.

### Keeps the last known catalog offline

`{#catalog::last-known-catalog-offline}`

When the catalog is refreshed successfully, it replaces the locally saved copy. When eternalfest.net can't be reached, the last saved catalog is shown and marked as offline, never an empty list.

### First launch without network

`{#catalog::first-launch-without-network}`

When eternalfest.net can't be reached and no catalog was ever saved, the library shows the contrées already in the cache (none on a fresh install), with a clear "can't reach Eternalfest" message and a retry action.

## Acceptance criteria

- Given eternalfest.net lists 45 public contrées over 3 pages, when the catalog is refreshed, then all 45 are listed.
- Given eternalfest.net lists no public contrée, when the catalog is refreshed, then the catalog is empty and no error is shown.
- Given the recorded details of `hammerfest-deluxe`, when they are read, then the build exposes version `2.5.1`, loader `5.1.2`, a custom engine blob, a patcher blob, and its modes and families.
- Given a build whose engine is `V96`, when it is read, then it is marked as using the base engine and has no engine blob.
- Given a saved catalog and no network, when the library opens, then the saved catalog is shown with an offline notice.
- Given no saved catalog and no network, when the library opens, then an explanatory message and a retry action are shown.
- Given eternalfest.net answers with malformed data for one contrée, when the catalog is refreshed, then that contrée is skipped and logged, and the others are listed.

## Out of scope

- Private or unlisted contrées.
- Favorites, sorting by popularity, and categories (maybe later).
- Retry policies beyond a manual retry.
