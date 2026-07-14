# Bannerlord Troop Classifier

Bannerlord Troop Classifier is a lightweight shared dependency for mods that need
consistent tactical troop roles. It classifies real battle equipment rather than
troop-tree labels, so a troop's rolled loadout determines its battlefield role.

It currently provides Light Infantry, Shield Infantry, Shock Infantry,
Skirmisher, Pike Infantry, Foot Archer, Crossbowman, Melee Cavalry, and Horse
Archer roles. Pilum and other thrown polearms are deliberately distinct from
ordinary javelins.

The module has no settings and does not change gameplay by itself. Consumer mods
own their UI, saved configuration, and behaviour; Troop Classifier owns only the
shared role contract and equipment rules.

## Installation

Install this module alongside any mod that declares it as a requirement, then
enable it before those mods in the Bannerlord launcher.
