# Changelog

## [0.6.0](https://github.com/cmnemoi/eternalfest-desktop/compare/v0.5.0...v0.6.0) (2026-09-27)


### Features

* **play:** play in the Flash projector on macOS too ([132cb38](https://github.com/cmnemoi/eternalfest-desktop/commit/132cb389f20b0eb1d5d1bc53c28aecbb0b1381fb))


### Bug Fixes

* **packaging:** let autosplitters read Ruffle and the Flash projector on macOS ([8f5959c](https://github.com/cmnemoi/eternalfest-desktop/commit/8f5959cdefaba131e1f4103793caa7bab6afa893))
* **play:** fill the screen height on Retina Macs too ([6160586](https://github.com/cmnemoi/eternalfest-desktop/commit/61605864a2bf34783f332e504b9e76ff1bcb2bfe))
* **play:** quit the Flash projector when its window closes on macOS ([b579046](https://github.com/cmnemoi/eternalfest-desktop/commit/b57904691c6766984f82ba8ef858e7aabd8b557e))

## [0.5.0](https://github.com/cmnemoi/eternalfest-desktop/compare/v0.4.0...v0.5.0) (2026-09-26)


### Features

* **packaging:** Support MacOS and add installers ([ba1766b](https://github.com/cmnemoi/eternalfest-desktop/commit/ba1766be1f3b09362c7422463a5bb596456ea066))

## [0.4.0](https://github.com/cmnemoi/eternalfest-desktop/compare/v0.3.0...v0.4.0) (2026-09-26)


### Features

* **backend:** accept the loader URL carrying the whole run ([9624174](https://github.com/cmnemoi/eternalfest-desktop/commit/962417419d6c419023d9e0ed89516e5aa2e8b201))
* **play:** play contrées in Adobe's Flash projector on Linux ([f10c6e3](https://github.com/cmnemoi/eternalfest-desktop/commit/f10c6e32ba7ac59a6cba95e6ca375c68230d3919))
* **play:** play in the Flash projector on Windows too ([4b67edc](https://github.com/cmnemoi/eternalfest-desktop/commit/4b67edc645bb303b16d77f1c8e28db3d29599109))

## [0.3.0](https://github.com/cmnemoi/eternalfest-desktop/compare/v0.2.0...v0.3.0) (2026-09-26)


### Features

* **play:** open the game as high as the launcher's screen allows ([5d416a7](https://github.com/cmnemoi/eternalfest-desktop/commit/5d416a74c025b2be21a2b90f3fceac8e01541fd7))

## [0.2.0](https://github.com/cmnemoi/eternalfest-desktop/compare/v0.1.0...v0.2.0) (2026-09-26)


### Features

* **play:** close the game window when the game ends, and sum it up ([4e51605](https://github.com/cmnemoi/eternalfest-desktop/commit/4e51605644080512550fadf8fdc84205e381cad0))

## 0.1.0 (2026-09-26)


### Features

* **backend:** serve contrées to the loader from an embedded local server ([3265b0f](https://github.com/cmnemoi/eternalfest-desktop/commit/3265b0f7dfda4b1b683ba92c9fac7bafd1aff665))
* **catalog:** browse the last catalog offline and update contrées ([c458df0](https://github.com/cmnemoi/eternalfest-desktop/commit/c458df08fe3f9ffa158e7ab6acf58b8772ffc89b))
* **catalog:** read public contrées from the Eternalfest API ([13bb78e](https://github.com/cmnemoi/eternalfest-desktop/commit/13bb78e940520bcc7c709f68482882c2716a0e9c))
* **domain:** unlock a contrée through quests for a player profile ([03be56b](https://github.com/cmnemoi/eternalfest-desktop/commit/03be56b4cf0539a12419820495aa0e36e7a23646))
* **play:** play a downloaded contrée in a pinned Ruffle ([4a1b2ee](https://github.com/cmnemoi/eternalfest-desktop/commit/4a1b2eef1b931903265e02198289cc58cb45e873))
* **play:** play the complete profile by default ([918f99b](https://github.com/cmnemoi/eternalfest-desktop/commit/918f99b73ce34fd4e1233a2b51be66aa00f9bff9))
* **quests:** bundle the quests eternalfest.net hardcodes ([ea68276](https://github.com/cmnemoi/eternalfest-desktop/commit/ea68276ba0f04c6f5a68cc09a1f683c52da0c249))
* **quests:** list the items of a contrée's cached content ([80704be](https://github.com/cmnemoi/eternalfest-desktop/commit/80704beca05dddd302bd1a030765e57fe807da62))
* **store:** download contrées once into a verified local cache ([3e5c5b0](https://github.com/cmnemoi/eternalfest-desktop/commit/3e5c5b0ed080c38f6f41d2b91af25cef1a447531))
* **ui:** add the app to the Linux applications menu ([5813a35](https://github.com/cmnemoi/eternalfest-desktop/commit/5813a35702481819a9a1ff7d339c8ea814f6b6f2))
* **ui:** an app icon made from the Eternalfest logo ([347adaa](https://github.com/cmnemoi/eternalfest-desktop/commit/347adaab281f6cdd96559946fa66502ec11e9b56))
* **ui:** library and contrée page in an Avalonia app ([5d00185](https://github.com/cmnemoi/eternalfest-desktop/commit/5d00185bd7b4bb6f83e4debc4c07bdbe56f74148))
* **ui:** pick the player profile on the contrée page ([c6f7009](https://github.com/cmnemoi/eternalfest-desktop/commit/c6f7009b08683604aa8a01bde7b680ee7f7638ce))
* **ui:** play again, remembered choices, settings and file logs ([6a167a4](https://github.com/cmnemoi/eternalfest-desktop/commit/6a167a4f2dc83b79c3c9f17ab93c5f61f1487dba))


### Bug Fixes

* **build:** check out text files with LF on every system ([3527cb6](https://github.com/cmnemoi/eternalfest-desktop/commit/3527cb66159bcf6b9d3730e16f368b93aac3be83))
* **build:** package for this machine by default, and explain a version argument ([0096ed0](https://github.com/cmnemoi/eternalfest-desktop/commit/0096ed07ac898d72ca263296ef203464c99aae65))
* **build:** run the tests after the format check in mise run ci ([499a575](https://github.com/cmnemoi/eternalfest-desktop/commit/499a575ad8d4038c6fc62a185b89c37028a5b29f))
