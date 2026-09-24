# Game store (download and cache)

## Why

Once downloaded, a contrée must be playable forever without network, without downloading the same bytes twice, and without ever running corrupted files.

## Scope

Downloading every file of a contrée build into the local cache, checking them, reporting progress, detecting newer builds, and clearing the cache.

## Rules

### Downloads a complete build

`{#store::downloads-complete-build}`

Downloading a contrée stores its details and every blob its build references: the custom engine (when present), patcher, content, content translations, musics and icon.

### Verifies each blob

`{#store::verifies-blob-digest}`

Each blob is checked against its SHA-256 digest and byte size before it is kept. A mismatch discards the file and fails the download.

### A contrée is downloaded only when complete

`{#store::downloaded-only-when-complete}`

A contrée counts as downloaded only once all its blobs are stored and verified. An interrupted or failed download leaves it "not downloaded". Retrying keeps the blobs already verified.

### Never downloads a blob twice

`{#store::never-downloads-twice}`

Blobs are immutable and identified by id: a blob already in the cache is never fetched again, even when it is shared between builds or contrées.

### Reports progress

`{#store::reports-progress}`

While downloading, progress is reported as downloaded bytes out of the build's total byte size.

### Detects a newer build

`{#store::detects-newer-build}`

When the catalog shows a different active build version than the downloaded one, the contrée is marked "update available". The downloaded build stays playable until the player chooses to update. Updating replaces it only after the new build is fully downloaded.

### Clears the cache

`{#store::clears-cache}`

Clearing the cache removes every downloaded contrée and blob. It is refused while a game is running.

## Acceptance criteria

- Given a build with 1 engine, 1 patcher, 1 content, 1 translation and 2 musics, when it is downloaded, then 6 blobs plus the icon are stored and the contrée is downloaded.
- Given a blob whose bytes don't match its digest, when the build is downloaded, then the download fails, the blob isn't kept and the contrée isn't downloaded.
- Given a download interrupted after 3 of 6 blobs, when it is retried, then only the 3 missing blobs are fetched.
- Given a downloaded contrée, when it is downloaded again, then no request is made.
- Given a downloaded build `2.5.1` and a catalog advertising `2.6.0`, when the library shows it, then it is playable and marked "update available".
- Given an update that fails halfway, when the player plays the contrée, then build `2.5.1` is still used.
- Given a running game, when the player clears the cache, then the action is refused with an explanation.
- Given the disk is full, when a blob is written, then the download fails explicitly and the contrée stays not downloaded.

## Out of scope

- Keeping several builds of one contrée side by side (speedrunner use case, later).
- Garbage collecting blobs no longer referenced by any build (clearing the cache is enough for v1).
- Parallel downloads of several contrées.
