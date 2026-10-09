using System;
using System.Collections.Generic;
using UnityEngine;

namespace NhxSimpleDeposit
{
    internal static class InventorySortService
    {
        internal static bool CanSort(Container container)
        {
            if (container == null || Player.m_localPlayer == null)
            {
                return false;
            }

            if (ContainerService.IsTombstone(container))
            {
                return false;
            }

            return ContainerService.IsAccessibleContainer(container);
        }

        internal static void SortContainer(Container container)
        {
            if (!CanSort(container))
            {
                return;
            }

            Inventory inventory = container.GetInventory();

            if (inventory == null)
            {
                return;
            }

            if (!ContainerService.EnsureOwnership(container))
            {
                return;
            }

            Sort(inventory);
        }

        private static void Sort(Inventory inventory)
        {
            List<ItemDrop.ItemData> allItems = inventory.GetAllItems();

            if (allItems == null || allItems.Count <= 1)
            {
                return;
            }

            int width = inventory.GetWidth();
            int height = inventory.GetHeight();

            if (width <= 0 || height <= 0)
            {
                return;
            }

            List<ItemDrop.ItemData> itemsToSort = new List<ItemDrop.ItemData>(allItems.Count);
            HashSet<(int x, int y)> reservedSlots = new HashSet<(int x, int y)>();

            foreach (ItemDrop.ItemData item in allItems)
            {
                if (item == null)
                {
                    continue;
                }

                if (!IsValidSlot(item.m_gridPos, width, height))
                {
                    continue;
                }

                if (item.m_equipped)
                {
                    reservedSlots.Add((item.m_gridPos.x, item.m_gridPos.y));
                    continue;
                }

                itemsToSort.Add(item);
            }

            bool changed = MergeStacks(inventory, itemsToSort);

            itemsToSort.Sort(CompareItems);

            int itemIndex = 0;

            for (int y = 0; y < height && itemIndex < itemsToSort.Count; y++)
            {
                for (int x = 0; x < width && itemIndex < itemsToSort.Count; x++)
                {
                    if (reservedSlots.Contains((x, y)))
                    {
                        continue;
                    }

                    ItemDrop.ItemData item = itemsToSort[itemIndex];
                    Vector2i position = new Vector2i(x, y);

                    if (item.m_gridPos != position)
                    {
                        item.m_gridPos = position;
                        changed = true;
                    }

                    itemIndex++;
                }
            }

            // An already sorted chest isn't saved again: every save is sent to the other players and overwrites the
            // chest for them, so it's only worth doing for a real change.
            if (changed)
            {
                ContainerService.NotifyInventoryChanged(inventory);
            }
        }

        // Returns whether any items were moved between stacks.
        private static bool MergeStacks(Inventory inventory, List<ItemDrop.ItemData> itemsToSort)
        {
            if (itemsToSort.Count <= 1)
            {
                return false;
            }

            bool merged = false;

            Dictionary<(string name, int quality, int worldLevel), List<ItemDrop.ItemData>> groups = new Dictionary<(string, int, int), List<ItemDrop.ItemData>>();

            foreach (ItemDrop.ItemData item in itemsToSort)
            {
                if (item.m_shared.m_maxStackSize <= 1)
                {
                    continue;
                }

                // World level too, like the game's own stacking: merging would silently move items to another world level.
                (string name, int quality, int worldLevel) key = (item.m_shared.m_name, item.m_quality, item.m_worldLevel);

                if (!groups.TryGetValue(key, out List<ItemDrop.ItemData> group))
                {
                    group = new List<ItemDrop.ItemData>();
                    groups[key] = group;
                }

                group.Add(item);
            }

            foreach (List<ItemDrop.ItemData> group in groups.Values)
            {
                if (group.Count <= 1)
                {
                    continue;
                }

                int maxStackSize = group[0].m_shared.m_maxStackSize;
                int targetIndex = 0;

                for (int sourceIndex = 1; sourceIndex < group.Count; sourceIndex++)
                {
                    ItemDrop.ItemData source = group[sourceIndex];

                    while (source.m_stack > 0 && targetIndex < sourceIndex)
                    {
                        ItemDrop.ItemData target = group[targetIndex];
                        int room = maxStackSize - target.m_stack;

                        if (room <= 0)
                        {
                            targetIndex++;
                            continue;
                        }

                        int amount = Mathf.Min(room, source.m_stack);
                        target.m_stack += amount;
                        source.m_stack -= amount;
                        merged = true;
                    }
                }
            }

            for (int i = itemsToSort.Count - 1; i >= 0; i--)
            {
                if (itemsToSort[i].m_stack <= 0)
                {
                    inventory.RemoveItem(itemsToSort[i]);
                    itemsToSort.RemoveAt(i);
                }
            }

            return merged;
        }

        private static bool IsValidSlot(Vector2i position, int width, int height)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        private static int CompareItems(ItemDrop.ItemData a, ItemDrop.ItemData b)
        {
            int result = a.m_shared.m_itemType.CompareTo(b.m_shared.m_itemType);

            if (result != 0)
            {
                return result;
            }

            result = string.Compare(a.m_shared.m_name, b.m_shared.m_name, StringComparison.Ordinal);

            if (result != 0)
            {
                return result;
            }

            result = b.m_quality.CompareTo(a.m_quality);

            if (result != 0)
            {
                return result;
            }

            result = b.m_stack.CompareTo(a.m_stack);

            if (result != 0)
            {
                return result;
            }

            // List.Sort isn't stable, so items that tie on everything above (e.g. two identical swords) would
            // otherwise swap places every time the chest is opened. Their current order breaks the tie instead.
            result = a.m_gridPos.y.CompareTo(b.m_gridPos.y);

            if (result != 0)
            {
                return result;
            }

            return a.m_gridPos.x.CompareTo(b.m_gridPos.x);
        }
    }
}
