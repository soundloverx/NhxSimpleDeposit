using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System.Linq;
using UnityEngine;

namespace NhxSimpleDeposit
{
    // What protects items from quick stack: AzuExtendedPlayerInventory's favorites when it's installed, otherwise this
    // mod's own copy of them (PlayerFavorites, plus the patches that let the player set them), which works like AzuEPI's:
    // hold the modifier key and left-click an item to favorite its item type, or right-click a slot to favorite the slot.
    internal static class FavoritesService
    {
        private const string AzuAutoStoreGuid = "Azumatt.AzuAutoStore";
        private const string QuickStackStoreGuid = "goldenrevolver.quick_stack_store";

        internal static bool UseOwnFavorites => !AzuEpiService.IsInstalled;

        // Same as AzuEPI: AzuAutoStore and Quick Stack Store come with this same favoriting, so with either of them
        // installed the clicks, borders, tooltips and Stack All are left to them. Both handling the same click would
        // toggle the favorite twice.
        internal static bool IsOwnUiActive => UseOwnFavorites
            && !Chainloader.PluginInfos.ContainsKey(AzuAutoStoreGuid)
            && !Chainloader.PluginInfos.ContainsKey(QuickStackStoreGuid);

        internal static string UnavailableMessage => UseOwnFavorites ? "Favorites unavailable" : AzuEpiService.FavoritesUnavailableMessage;

        // Favorites that can't be read: quick stack stands down rather than ignoring them
        internal static bool AreUnavailable(Player player)
        {
            if (!UseOwnFavorites)
            {
                return AzuEpiService.IsBroken;
            }

            return player != null && PlayerFavorites.Get(player.GetPlayerID()).IsUnreadable;
        }

        internal static bool IsFavorited(Player player, ItemDrop.ItemData item)
        {
            if (!UseOwnFavorites)
            {
                return AzuEpiService.IsFavorited(player, item);
            }

            return item != null && player != null && PlayerFavorites.Get(player.GetPlayerID()).IsItemOrSlotFavorited(item);
        }

        internal static bool IsModifierKeyHeld()
        {
            KeyboardShortcut shortcut = Plugin.FavoritingModifierKey;

            return shortcut.MainKey != KeyCode.None
                && ZInput.GetKey(shortcut.MainKey, false)
                && shortcut.Modifiers.All(key => ZInput.GetKey(key, false));
        }
    }
}
