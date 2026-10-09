# Changelog

## v1.0.0
- Quick Stack (keyboard shortcut) and chest sorting on open, split out of NhxQualityPack, without NhxQualityPack's Q and S inventory buttons
- Your own inventory is no longer sorted (that was only available through the S button)
- Quick Stack only deposits into player-built chests, never into dungeon or other world chests
- Items are protected by favorites (item types and slots) instead of NhxQualityPack's locks: AzuExtendedPlayerInventory's when it's installed, otherwise the mod's own copy of them, with the same keys, borders, tooltips, Stack All protection and favorites file
- AzuEPI's equipment and quick slots are never quick stacked, and its extra inventory rows are treated like the rest of the inventory
- Tombstones are never quick stacked into or sorted
- Chests are refreshed from the latest network data before being sorted or quick stacked into, so another player's change from a moment ago isn't undone
- Sorting keeps identical items in their order instead of shuffling them on every open, and doesn't save a chest that was already sorted
