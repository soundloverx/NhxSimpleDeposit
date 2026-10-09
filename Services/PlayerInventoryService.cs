using UnityEngine;

namespace NhxSimpleDeposit
{
    internal static class PlayerInventoryService
    {
        // Rows of the player inventory that hold ordinary items. Rows past these belong to other mods
        // (AzuEPI's equipment and quick slots, hidden rows) and are never touched.
        internal static int GetNormalRows(Player player)
        {
            Inventory inventory = player.GetInventory();
            int azuEpiRows = AzuEpiService.GetNormalRows(inventory);

            if (azuEpiRows >= 0)
            {
                return Mathf.Clamp(azuEpiRows, 0, inventory.GetHeight());
            }

            return GetVanillaHeight(player);
        }

        private static int GetVanillaHeight(Player player)
        {
            if (player.TryGetUniqueKeyValue(Player.InventoryRowsKey, out string value) && int.TryParse(value, out int rows))
            {
                return Mathf.Clamp(rows, 0, 9);
            }

            return 4;
        }
    }
}
