using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NhxSimpleDeposit.Patches
{
    // Setting favorites by clicking, and showing them, the same way AzuEPI does. Only patched in when AzuEPI isn't
    // installed, it does all of this itself.
    [HarmonyPatch(typeof(InventoryGrid))]
    internal static class InventoryGridPatch
    {
        // InventoryGrid.GetButtonPos(GameObject) and InventoryGui.m_dragGo are private in the actual game assembly loaded at runtime
        private static readonly MethodInfo GetButtonPosMethod = AccessTools.Method(typeof(InventoryGrid), "GetButtonPos", new[] { typeof(GameObject) });
        private static readonly FieldInfo DragGoField = AccessTools.Field(typeof(InventoryGui), "m_dragGo");

        private static bool Prepare()
        {
            return FavoritesService.UseOwnFavorites;
        }

        // Modifier key + left-click on an item favorites its item type
        [HarmonyPatch(nameof(InventoryGrid.OnLeftDown))]
        [HarmonyPrefix]
        private static bool OnLeftDownPrefix(InventoryGrid __instance, UIInputHandler clickHandler)
        {
            return !TryToggleFavorite(__instance, clickHandler, true);
        }

        // Modifier key + right-click on a slot favorites the slot, empty or not
        [HarmonyPatch(nameof(InventoryGrid.OnRightDown))]
        [HarmonyPrefix]
        private static bool OnRightDownPrefix(InventoryGrid __instance, UIInputHandler element)
        {
            return !TryToggleFavorite(__instance, element, false);
        }

        // Runs every frame while the inventory is open, so a toggled favorite shows right away
        [HarmonyPatch(nameof(InventoryGrid.UpdateGui))]
        [HarmonyPostfix]
        private static void UpdateGuiPostfix(InventoryGrid __instance, Player player)
        {
            UI.FavoriteBorderRenderer.Refresh(__instance, player);
        }

        // Returns whether the click was used for favoriting, in which case the game doesn't also handle it.
        private static bool TryToggleFavorite(InventoryGrid grid, UIInputHandler clickHandler, bool toggleItem)
        {
            InventoryGui inventoryGui = InventoryGui.instance;
            Player player = Player.m_localPlayer;

            if (inventoryGui == null || grid != inventoryGui.m_playerGrid || player == null || player.IsTeleporting())
            {
                return false;
            }

            // a click while dragging an item drops it, as usual
            if (DragGoField?.GetValue(inventoryGui) is GameObject dragGo && dragGo != null)
            {
                return false;
            }

            if (!FavoritesService.IsOwnUiActive || !FavoritesService.IsModifierKeyHeld() || GetButtonPosMethod == null || clickHandler == null)
            {
                return false;
            }

            Vector2i position = (Vector2i)GetButtonPosMethod.Invoke(grid, new object[] { clickHandler.gameObject });

            if (position.x < 0 || position.y < 0)
            {
                return false;
            }

            PlayerFavorites favorites = PlayerFavorites.Get(player.GetPlayerID());

            if (!toggleItem)
            {
                favorites.ToggleSlot(position);
                return true;
            }

            ItemDrop.ItemData item = grid.GetInventory()?.GetItemAt(position.x, position.y);

            if (item == null)
            {
                return false;
            }

            favorites.ToggleItem(item);
            return true;
        }
    }
}
