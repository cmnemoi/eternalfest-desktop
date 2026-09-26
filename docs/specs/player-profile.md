# Player profile

## Why

Offline, nobody is logged in, so every game used to start like a brand new Eternalfest account: the build's starter families and an empty inventory. In *Les Cavernes de Hammerfest*, that means one life, one bomb, and no *Intuition* option. Eternalfest unlocks all of this through quests, from the items a player collected over years. Offline players should get the complete game by default, and still be able to start from scratch.

## Scope

The two player profiles, how the complete profile unlocks a contrée the way eternalfest.net does for a logged-in player, and the inventory it gives the loader. Choosing the profile is covered by the launcher-ui spec, and serving it by the offline-backend spec.

## Rules

### Two profiles, complete by default

`{#profile::complete-by-default}`

The player picks one profile per game:

- **Complete profile**: every quest of the contrée is completed.
- **New player**: nothing is unlocked, like a guest on eternalfest.net.

The complete profile is used when nothing was chosen, when a remembered choice has no profile, and when it names a profile this version doesn't know.

### A new player gets the published contrée

`{#profile::new-player-as-published}`

With the new player profile, the families are the build's families as published, the inventory is empty, and modes and options keep their published visibility. No quest is applied.

### The complete profile owns every item

`{#profile::complete-inventory}`

With the complete profile, the inventory holds 9999 of every item the contrée's content lists, and of every item one of its quests requires. Each item appears once.

### Quests unlock like on eternalfest.net

`{#profile::unlocks-like-eternalfest}`

Starting from the published build, the contrée's quests are applied in their order. A quest is complete when the inventory holds at least the required quantity of each item it requires. A quest that requires nothing is always complete. A complete quest's rewards:

- give or remove a family;
- make a mode visible, or hide it;
- make an option visible and enabled in every mode that has it, or disable it.

A reward naming a mode or option the build doesn't have changes nothing. The families are then listed in increasing order, each once, separated by commas.

### Quests come with the launcher

`{#profile::bundled-quests}`

eternalfest.net only knows quests for three contrées, hardcoded on the server: `hammerfest` (*Les Cavernes de Hammerfest*), `otherworldly_well` and `hackfest`. The launcher bundles the same quests, matched by the contrée key. Any other contrée has no quest. Contrées that run their own quests in their scripts read the inventory themselves.

### Unreadable content degrades, never blocks

`{#profile::unreadable-content}`

When the complete profile can't read the items of the contrée's content (missing from the cache, or not valid XML), the game still starts. The inventory then holds only the items its quests require, so families, modes and options are the same. The problem is logged.

## Acceptance criteria

- Given no choice, when a game starts, then the complete profile is used.
- Given remembered choices without a profile, or with the profile `"Legendary"`, when they are read, then the complete profile is used and the other choices are kept.
- Given *Les Cavernes de Hammerfest* and the new player profile, then the families are `0,7,13,15,18,1000,1028,5015`, the inventory is empty and *Intuition* is hidden.
- Given *Les Cavernes de Hammerfest* and the inventory of a recorded Eternalfest player, when it is unlocked, then the families are exactly the ones eternalfest.net gave that player, and the modes and options match the ones it showed that player.
- Given *Les Cavernes de Hammerfest* and the complete profile, then the families hold 100 (second bomb), 102, 103, 104, 105 and 108 (five extra lives), hold no family a quest removes (5, 6, 7, 8, 9, 13 to 18, 1021, 1022, 1028), and *Intuition* and the *Time Attack* mode are offered.
- Given a quest requiring 150 of an item, when the inventory holds 150, then it is complete, and with 149 it isn't.
- Given two quests giving the same family, then the family is listed once.
- Given a quest giving a family and a later quest removing it, then the family is absent.
- Given `otherworldly_well` and the complete profile, then *Cauchemar* is offered: the always-complete first quest disables it, and a later quest gives it back.
- Given a quest giving an option or mode the build doesn't have, then no option or mode is added and nothing fails.
- Given a contrée whose key has no quests, or no key, and the complete profile, then the families are the build's, in increasing order, and the inventory holds 9999 of every item of its content.
- Given content listing items `1, 1, x, 2` and the complete profile, then the inventory holds items 1 and 2 once each.
- Given content with no items and a contrée without quests, then the inventory is empty. Given a build with no families, then the families are an empty string.
- Given the complete profile and content that isn't XML, then the game starts, the inventory holds exactly the items the quests require, and a warning is logged.
- Given the bundled quests, when they are loaded, then each of the three keys has quests and no key appears twice.

## Out of scope

- Choosing which quests or families to unlock (a custom profile).
- Rewards eternalfest.net doesn't apply either: tokens, bank slots and logs mentioned in the original Hammerfest quests.
- Keeping the real item counts of a player, or growing them from finished games (results are discarded).
- Quests of contrées other than the three above: eternalfest.net has none on the server, and their scripts handle them from the inventory.
- Making the new player profile closer to a real new account (for `hackfest`, eternalfest.net hides the *prelude* mode from logged-in players until a quest gives it; guests and new players offline see it).
