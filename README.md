# NhxSimpleDeposit

A private mod for personal use with friends - not published on Thunderstore.

Store items from your inventory into nearby chests you have access to with a keyboard shortcut, within a configurable radius. Also sorts any chest you open. Items you've [favorited](#favorites) are left alone: through **AzuExtendedPlayerInventory** (AzuEPI) when it's installed, otherwise through the mod's own copy of AzuEPI's favorites, which works the same way. The mod adds no buttons of its own.

These are the Quick Stack and Sort features of **NhxQualityPack**, split out into a small mod of their own so it can be used alongside other mods. Don't install both: NhxSimpleDeposit won't load while NhxQualityPack is installed. NhxQualityPack's item locks are not carried over, favorite those items again (see [Favorites](#favorites)).

The quick stack/sort features were originally forked from Hex_Viking's **HexQuickStackStorage** (itself inspired by Goldenrevolver's **Quick Stack Store Sort Trash**).

## Instructions

### Quick Stack

Press the configured Quick Stack keyboard shortcut (default `P`).

Items will only be moved if:

- The item is not equipped
- The item is not in your hotbar
- The item is not [favorited](#favorites), and isn't in a favorited slot
- The item is inside the normal player inventory (not in AzuEPI's equipment or quick slots)
- A nearby chest you have access to already contains that item type
- That chest was built by a player (dungeon and other world chests are never deposited into)
- That chest isn't a tombstone
- The chest has at least some room for the item
- Nobody else currently has that chest open

Items will not be stored in empty chests or chests that do not already contain that item.

If a chest only has room for part of a stack (e.g. topping off an existing stack of 35/50), that partial amount is deposited and the rest stays in your inventory, rather than skipping the item entirely.

A message is shown summarizing what happened:

- `All items deposited` - everything that could be moved was moved.
- `x/y items deposited` - only part of what you carried was moved. `y` is every item you held that was worth considering, including ones that don't yet exist in any nearby chest; `x` is how many of those actually got deposited.
- `No room for items` - some of your items already exist in a nearby chest, but none of those chests had space.
- `No matching containers` - none of your items exist in any nearby chest yet. Quick Stack only tops off an item type a chest already holds; it never seeds a chest with a type it doesn't have.
- `No available containers` - no accessible player-built chest was found within the search radius.
- Nothing is shown when there's nothing in your inventory worth considering at all (e.g. it's empty, or everything left is equipped or favorited).

### Sort

Every chest is sorted automatically the moment you open it. There's nothing to click; your own inventory and tombstones are never sorted.

Chests are sorted completely: favorites only apply to your own inventory, so favorited item types inside a chest are sorted like everything else.

Partial stacks of the same item type and quality (and the same world level, as the game itself requires for stacking) are combined into as few stacks as possible first (e.g. two stacks of 10 Wood become one stack of 20), freeing up slots before the remaining items are placed.

Items that sort the same (e.g. two identical swords) keep their order, so a sorted chest doesn't get shuffled each time you open it. A chest that's already sorted isn't saved again.

### Favorites

Items are protected with favorites. With **AzuExtendedPlayerInventory** installed, its own favorites are used. Without it, this mod brings its own copy of AzuEPI's favorites, which works exactly the same way. With the default `Favoriting Modifier Key` (Left Alt):

- Hold **Left Alt** and **left-click** an item to favorite that item type (every stack of it, wherever it is in your inventory)
- Hold **Left Alt** and **right-click** a slot to favorite the slot (whatever sits in it, even if it's empty for now)

Do the same again to remove the favorite. A coloured border is drawn around favorited items (yellow) and slots (blue), green when both apply, and the item's tooltip mentions it.

Favorited items and anything in a favorited slot are:

- Skipped by Quick Stack
- Kept out of the vanilla chest **Stack All** button (and holding the use key on a chest)

Favorites are saved per character, on your computer, in `BepInEx\config\AzuExtendedPlayerInventory_player_<character id>.dat`. Without AzuEPI the mod uses that same file, so your favorites are kept when you install or remove AzuEPI. If you've used AzuAutoStore, its copy of the favorites is merged in too, as AzuEPI does.

Without AzuEPI, the modifier key, border colours and tooltip hint can be changed in this mod's config (the same settings AzuEPI has). With AzuEPI installed, those settings don't show up here, AzuEPI's own `10 - Favoriting` settings apply. Like AzuEPI's, the mod's favoriting stands aside while AzuAutoStore or Quick Stack Store is installed: they have the same favoriting, use their key instead.

If the favorites can't be read (AzuEPI changed in an update so that this mod can't read its favorites any more, or the favorites file can't be read), Quick Stack is switched off (`AzuEPI favorites unavailable` or `Favorites unavailable` is shown, and the reason is in the BepInEx log) rather than moving favorited items. Opened chests are still sorted.

## Container Access

Quick Stack works with any player-built container you currently have access to, not only ones you personally built.

A container inside another player's ward that you aren't permitted in is skipped, the same way opening it by hand would be blocked. A container in your own ward, in a ward you're permitted in, or with no ward at all, is fair game. A chest someone has explicitly set to Private is also skipped unless you're its creator, again matching normal chest rules.

A chest another player currently has open is left alone entirely for that pass, rather than claiming it out from under them.

Naturally generated/world containers (dungeon loot chests, chests in world locations, including the ones added by location mods) are never deposited into, even if they already hold an item type you carry. They are still sorted when you open them, like any other chest.

Tombstones are never deposited into and never sorted: they're left exactly as they were when you died.

## Compatibility

- **AzuExtendedPlayerInventory**: optional, see [Favorites](#favorites) for what changes with it. AzuEPI's equipment and quick slots and its `Extra Inventory Rows` are recognized: extra rows are quick stacked like the rest of your inventory, the equipment and quick slots never are
- **NhxQualityPack**: not compatible (it already contains these features), NhxSimpleDeposit won't load while it's installed
- Inventory rows bought from the trader count as part of your inventory
- The hotbar is always the first inventory row, and is never quick stacked
- **NhxHotbarSwap**: swaps the first two rows, and moves slot favorites (AzuEPI's or this mod's own) along with their items. The second row, where the swapped-out hotbar ends up, is quick stacked like any other row: to keep both hotbar setups out of it, favorite their item types, or the slots of both rows
- **HotbarSwitch**: it only changes which row the number keys use, so the row it switches to isn't protected; favorite what you keep there

## Multiplayer

- Client-side mod
- Does not have server sync
- Access to a container is checked using the same ward and privacy rules the base game uses
- Writing to a container claims network ownership of it first (mirroring how the game's own "Take All" does it), so items don't get silently lost when writing into a chest another peer currently owns
- Before sorting or depositing, the chest's contents are refreshed from the latest data received from the other players. The game itself only refreshes nearby chests once a second, so without this a change another player made a moment ago could be undone

## Configuration

Configuration options include:

- Search radius
- Quick Stack keyboard shortcut (default `P`)
- Without AzuEPI only: the favoriting modifier key (default `Left Alt`), the favorite border colours, and whether tooltips mention favorites
