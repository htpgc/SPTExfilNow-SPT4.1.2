# Changelog

## 1.1.0 - SPT 4.1.2

### Compatibility

- Updated the plugin for SPT 4.1.2.
- Reworked LocalGame discovery for the newer SPT/EFT environment.
- Reduced compile-time dependence on EFT types that may be renamed or obfuscated between builds.
- Preserved the normal raid-ending and settlement flow.
- Verified PMC extraction.
- Verified Scav extraction.
- Verified loot persistence.
- Verified quest item persistence.
- Verified quest progress.
- Verified experience settlement.

### Behavioral change

- The original SPTExfilNow randomly selected one active extraction point.
- This adaptation selects the first active extraction point returned by the game.

This difference only affects which extraction point name is used for settlement and does not affect the ability to extract immediately from the player's current position.
