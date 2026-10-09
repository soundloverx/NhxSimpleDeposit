using HarmonyLib;
using UnityEngine;

namespace NhxSimpleDeposit.Patches
{
    // AzuEPI's favorite line at the end of an item's tooltip. Only patched in when AzuEPI isn't installed, it adds its own.
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    internal static class ItemTooltipPatch
    {
        private const string FavoritedItemText = "Item is favorited and won't be stored";
        private const string FavoritedSlotText = "Slot is favorited and won't be stored";
        private const string FavoritedItemAndSlotText = "Item & Slot are favorited and won't be stored";

        private static bool Prepare()
        {
            return FavoritesService.UseOwnFavorites;
        }

        private static void Postfix(ItemDrop.ItemData item, bool crafting, bool appending, ref string __result)
        {
            // An item with another item's tooltip appended runs through here twice; the line is only added once, at the end
            if (crafting || appending || item == null || !Plugin.ShowFavoriteTooltip || !FavoritesService.IsOwnUiActive)
            {
                return;
            }

            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            PlayerFavorites favorites = PlayerFavorites.Get(player.GetPlayerID());
            bool itemFavorited = favorites.IsItemFavorited(item);

            // Unlike AzuEPI, which also says so for an item in a chest that happens to sit at a favorited position
            bool slotFavorited = player.GetInventory().ContainsItem(item) && favorites.IsSlotFavorited(item.m_gridPos);

            if (itemFavorited && slotFavorited)
            {
                __result += GetLine(Plugin.FavoritedItemAndSlotBorderColor, FavoritedItemAndSlotText);
            }
            else if (itemFavorited)
            {
                __result += GetLine(Plugin.FavoritedItemBorderColor, FavoritedItemText);
            }
            else if (slotFavorited)
            {
                __result += GetLine(Plugin.FavoritedSlotBorderColor, FavoritedSlotText);
            }
        }

        private static string GetLine(Color color, string text)
        {
            return $"\n<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }
    }
}
