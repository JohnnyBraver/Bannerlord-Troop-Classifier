# Changelog - Troop Classifier

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
