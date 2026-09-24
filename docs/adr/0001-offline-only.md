# 0001 — Offline only

- Status: accepted
- Date: 2026-09-24

## Context

Eternalfest is an online platform: Eternaltwin accounts, runs, scores, quests and inventories live on eternalfest.net. The official Eternaltwin desktop app already lets players play online.

What's missing is the experience of a local clone played through `eternaldev`: every contrée, every option, no account, no consequences. Today it takes a full development setup (see `hammerfest-tas`).

## Decision

Eternalfest Desktop only has an offline mode:

- No authentication: no `ef_sid` cookie, no Eternaltwin OAuth.
- No writes to eternalfest.net: runs are created locally, and results are accepted and discarded.
- The only calls to eternalfest.net are anonymous reads: the game list, game details and blobs.
- Progress is "just for fun": all families, all modes, and every option locked by progression are unlocked. The inventory is empty and no score is kept.

## Consequences

- No credentials to store or protect, and no risk of polluting official leaderboards.
- No overlap with the official Eternaltwin desktop app.
- Quest rewards and profile-dependent content that rely on a real account aren't available.
- If an online mode is ever wanted, it needs a new ADR (authentication, run creation on the real server).
