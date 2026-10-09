using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Reflection;

namespace NhxSimpleDeposit
{
    // When AzuExtendedPlayerInventory is installed, items are protected through its favorites (Left Alt + click by
    // default): a favorited item type, or anything in a favorited slot, is never quick stacked. Without it, our own copy
    // of its favorites is used instead (see FavoritesService). AzuEPI has no public API for favorites, so its UserConfig
    // class is reached through reflection, which also keeps AzuEPI optional. AzuEPI also appends its equipment and quick slots to the player inventory as extra
    // rows (and can add ordinary rows of its own), so its own row count is used to tell those apart.
    internal static class AzuEpiService
    {
        internal const string PluginGuid = "Azumatt.AzuExtendedPlayerInventory";

        internal const string FavoritesUnavailableMessage = "AzuEPI favorites unavailable";

        private const string UserConfigTypeName = "AzuEPI.Game.Favoriting.UserConfig";
        private const string LayoutTypeName = "AzuEPI.Core.InventoryHandlers.Layout";

        // static UserConfig UserConfig.GetPlayerConfig(long playerID)
        private static MethodInfo _getPlayerConfigMethod;

        // bool UserConfig.IsItemNameOrSlotFavorited(ItemDrop.ItemData item)
        private static MethodInfo _isItemOrSlotFavoritedMethod;

        // static int Layout.NormalRows(Inventory inv)
        private static MethodInfo _normalRowsMethod;

        private static bool _initialized;
        private static bool _installed;
        private static bool _broken;

        // AzuEPI is installed, but its favorites can't be read (e.g. renamed in an update). Quick stack stands down
        // then, rather than ignoring the player's favorites.
        internal static bool IsBroken
        {
            get
            {
                EnsureInitialized();
                return _broken;
            }
        }

        // Only asked when AzuEPI is installed. With unreadable favorites, everything counts as favorited, so an
        // operation that was already running when the favorites broke leaves the rest of the items where they are.
        internal static bool IsFavorited(Player player, ItemDrop.ItemData item)
        {
            if (item == null || !IsInstalled)
            {
                return false;
            }

            return Query(player, _isItemOrSlotFavoritedMethod, item);
        }

        // Rows of the player inventory that hold ordinary items, or -1 when AzuEPI isn't there to say.
        internal static int GetNormalRows(Inventory inventory)
        {
            EnsureInitialized();

            if (!_installed || _normalRowsMethod == null || inventory == null)
            {
                return -1;
            }

            try
            {
                return (int)_normalRowsMethod.Invoke(null, new object[] { inventory });
            }
            catch (Exception exception)
            {
                Plugin.Log.LogWarning($"Couldn't read AzuExtendedPlayerInventory's inventory rows, only the vanilla rows will be used: {exception}");
                _normalRowsMethod = null;
                return -1;
            }
        }

        internal static bool IsInstalled
        {
            get
            {
                EnsureInitialized();
                return _installed;
            }
        }

        private static bool Query(Player player, MethodInfo method, object argument)
        {
            if (_broken || player == null)
            {
                return true;
            }

            try
            {
                // AzuEPI keeps one UserConfig per player ID, so this hands back the live favorites, not a copy
                object userConfig = _getPlayerConfigMethod.Invoke(null, new object[] { player.GetPlayerID() });

                return (bool)method.Invoke(userConfig, new[] { argument });
            }
            catch (Exception exception)
            {
                MarkBroken($"reading the favorites failed: {exception}");
                return true;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out PluginInfo pluginInfo) || pluginInfo.Instance == null)
            {
                Plugin.Log.LogInfo("AzuExtendedPlayerInventory not found: using this mod's own favorites to protect items from quick stack (same keys and favorites file as AzuEPI's).");
                return;
            }

            _installed = true;

            Assembly assembly = pluginInfo.Instance.GetType().Assembly;
            Type userConfigType = assembly.GetType(UserConfigTypeName);
            Type layoutType = assembly.GetType(LayoutTypeName);

            if (userConfigType != null)
            {
                _getPlayerConfigMethod = AccessTools.Method(userConfigType, "GetPlayerConfig", new[] { typeof(long) });
                _isItemOrSlotFavoritedMethod = AccessTools.Method(userConfigType, "IsItemNameOrSlotFavorited", new[] { typeof(ItemDrop.ItemData) });
            }

            if (layoutType != null)
            {
                _normalRowsMethod = AccessTools.Method(layoutType, "NormalRows", new[] { typeof(Inventory) });
            }

            if (_normalRowsMethod == null)
            {
                Plugin.Log.LogWarning("Couldn't find AzuExtendedPlayerInventory's inventory rows, only the vanilla rows will be used.");
            }

            if (_getPlayerConfigMethod == null || _isItemOrSlotFavoritedMethod == null)
            {
                MarkBroken("its favorites weren't found (it may have changed in an update)");
                return;
            }

            Plugin.Log.LogInfo($"Using AzuExtendedPlayerInventory {pluginInfo.Metadata.Version} favorites to protect items from quick stack.");
        }

        private static void MarkBroken(string reason)
        {
            if (_broken)
            {
                return;
            }

            _broken = true;
            Plugin.Log.LogError($"AzuExtendedPlayerInventory is installed but {reason}. Quick stack is disabled so favorited items aren't moved; chests are still sorted.");
        }
    }
}
