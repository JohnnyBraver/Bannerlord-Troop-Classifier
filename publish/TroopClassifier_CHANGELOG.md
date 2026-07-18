# Changelog - Troop Classifier

## [v0.2.0] - 2026-07-18

### Added
- **Spear Infantry Role**: Foot troops equipped with a shield and a spear (melee polearm that is not a pike, javelin, or pilum) are now classified under `SpearInfantry` instead of generic `ShieldInfantry`.
- **Mounted Skirmisher Role**: Mounted troops equipped with throwing weapons but no bows/crossbows/slings are now classified under `MountedSkirmisher` instead of generic `MeleeCavalry`.
- **Priority Tie-Breakers**: Updated role priority ratings to favor specialized roles (`MountedSkirmisher` and `SpearInfantry`) over general ones when a troop template contains multiple loadout configurations.

## [v0.1.1] - 2026-07-14

### Added
- **Sling Coverage**: Sling-equipped troops now use the Foot Archer or Horse Archer role, according to whether their rolled loadout is mounted.
- **Deduplicated Classification Log**: `TroopClassifier_Log.txt` records each distinct troop/loadout variant once per mission, making live role decisions inspectable without producing one line per agent.

### Fixed
- **Crafted Javelins**: Normal javelins using Bannerlord's alternate javelin usage now count as javelins for skirmisher classification.
- **Pilum Distinction**: Throwable polearms such as pila remain excluded from javelin classification.

## [v0.1.0] - 2026-07-14

### Added
- Initial shared tactical role contract for Light Infantry, Shield Infantry, Shock Infantry, Skirmisher, Pike Infantry, Foot Archer, Crossbowman, Melee Cavalry, and Horse Archer.
- Spawned-agent classification based on actual rolled equipment, plus a stable troop-template fallback for campaign-side consumers.
- Weapon-aware skirmisher rules that distinguish ordinary javelins from Pilum and other throwable polearms.
- Bannerlord module packaging, build/deploy wrapper, version-bump validation, and automatic release-tag workflow.
