# 0007 — Bundle the quests eternalfest.net hardcodes

- Status: accepted
- Date: 2026-09-26

## Context

eternalfest.net unlocks item families, modes and options for a logged-in player from the items they own, through quests. The quests aren't in any contrée file: they are hardcoded in the Eternalfest server (`crates/core/src/inventory/quest_db.rs`, AGPL-3.0-or-later), for three contrées only (`hammerfest`, `otherworldly_well`, `hackfest`). The base engine only reads the resulting families: family 100 gives a second bomb, and families 102 to 105 and 108 give extra lives.

Offline, the launcher served the build's starter families and an empty inventory. Every game was then played like a brand new account: one life, one bomb, and options such as *Intuition* hidden (see the player-profile spec).

Alternatives considered:

- Serving a fixed "complete" family list per contrée, as the Cavernes sources do for local testing. It only covers one contrée and unlocks no mode or option.
- Giving every family found in the content XML, without quests. It keeps quest-item families in the drop pool, misses the perk families 100 to 114 (they are in no XML), and unlocks no mode or option.

## Decision

- Port the server's quest list to `src/EternalfestDesktop.Infrastructure/Quests/quests.json` with `eng/port-quests.cs`, and bundle it as a resource.
- Apply it the way the server does, with an inventory holding every item. The result is checked against a recorded logged-in session.
- Credit the source and its AGPL-3.0-or-later license in `THIRD-PARTY-NOTICES.md`. The GPL-3.0 allows combining with AGPL-3.0 code (section 13), and a quest table is arguably factual data anyway. Let the Eternalfest team know, without waiting for them to answer.

## Consequences

- The complete profile gets everything a player who finished every quest gets, for every contrée eternalfest.net knows quests for. Contrées with their own quests in their scripts get them from the inventory.
- When eternalfest.net changes its quests, the launcher stays on the bundled ones until `mise run port-quests` is run again and a new version is released. The script fails when the source format changes rather than porting part of it.
- If eternalfest.net ever stores quests in contrée builds, this table can go.
