using System.Collections.Generic;

namespace NhxSimpleDeposit
{
    internal static class QuickStackService
    {
        internal static QuickStackResult QuickStack(Player player)
        {
            if (player == null)
            {
                return new QuickStackResult(QuickStackOutcome.NothingToDeposit, 0, 0);
            }

            Inventory playerInventory = player.GetInventory();

            if (playerInventory == null)
            {
                return new QuickStackResult(QuickStackOutcome.NothingToDeposit, 0, 0);
            }

            if (FavoritesService.AreUnavailable(player))
            {
                return new QuickStackResult(QuickStackOutcome.FavoritesUnavailable, 0, 0);
            }

            List<Container> containers = ContainerService.GetNearbyContainers(player);

            if (containers.Count == 0)
            {
                return new QuickStackResult(QuickStackOutcome.NoContainersFound, 0, 0);
            }

            int normalRows = PlayerInventoryService.GetNormalRows(player);
            Dictionary<ItemDrop.ItemData, int> candidateItems = GetCandidateItems(player, playerInventory, normalRows);

            if (candidateItems.Count == 0)
            {
                // empty inventory, or everything left is favorited/equipped - nothing even worth checking containers for
                return new QuickStackResult(QuickStackOutcome.NothingToDeposit, 0, 0);
            }

            HashSet<ItemDrop.ItemData> matchedItems = new HashSet<ItemDrop.ItemData>();

            foreach (Container container in containers)
            {
                if (container == null)
                {
                    continue;
                }

                ContainerService.Reload(container);

                Inventory containerInventory = container.GetInventory();

                if (containerInventory == null)
                {
                    continue;
                }

                QuickStackIntoContainer(playerInventory, container, containerInventory, candidateItems, matchedItems);
            }

            // MoveItemToThis only reduces an item's m_stack when part of the stack is left
            // behind (partial deposit). When the whole stack is moved, the item is simply
            // removed from the player's inventory and m_stack is left untouched, so an item's
            // continued presence in the inventory is what tells full vs. partial vs. no deposit apart.
            HashSet<ItemDrop.ItemData> remainingItems = new HashSet<ItemDrop.ItemData>(playerInventory.GetAllItems());
            int total = 0;
            int deposited = 0;

            foreach (KeyValuePair<ItemDrop.ItemData, int> entry in candidateItems)
            {
                int originalStack = entry.Value;
                total += originalStack;
                deposited += remainingItems.Contains(entry.Key) ? originalStack - entry.Key.m_stack : originalStack;
            }

            if (deposited == 0 && matchedItems.Count == 0)
            {
                // none of the candidate items exist in any nearby container yet - nothing to top off
                return new QuickStackResult(QuickStackOutcome.NoMatchingItems, 0, 0);
            }

            if (deposited == 0)
            {
                // some items matched a nearby container's contents, but none of those containers had room
                return new QuickStackResult(QuickStackOutcome.NoRoomForItems, 0, 0);
            }

            return new QuickStackResult(QuickStackOutcome.ItemsDeposited, total, deposited);
        }

        private static Dictionary<ItemDrop.ItemData, int> GetCandidateItems(Player player, Inventory playerInventory, int normalRows)
        {
            Dictionary<ItemDrop.ItemData, int> candidateItems = new Dictionary<ItemDrop.ItemData, int>();

            foreach (ItemDrop.ItemData item in playerInventory.GetAllItems())
            {
                if (IsCandidateItem(player, item, normalRows))
                {
                    candidateItems[item] = item.m_stack;
                }
            }

            return candidateItems;
        }

        private static bool IsCandidateItem(Player player, ItemDrop.ItemData item, int normalRows)
        {
            if (item == null)
            {
                return false;
            }

            if (item.m_gridPos.y < 0 || item.m_gridPos.y >= normalRows)
            {
                return false;
            }

            if (item.m_gridPos.y == 0)
            {
                return false;
            }

            if (player.IsItemEquiped(item))
            {
                return false;
            }

            if (FavoritesService.IsFavorited(player, item))
            {
                return false;
            }

            return true;
        }

        private static void QuickStackIntoContainer(Inventory playerInventory, Container container, Inventory containerInventory, Dictionary<ItemDrop.ItemData, int> candidateItems, HashSet<ItemDrop.ItemData> matchedItems)
        {
            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(playerInventory.GetAllItems());
            bool ownershipEnsured = false;

            foreach (ItemDrop.ItemData item in items)
            {
                // a fully deposited item is gone from the inventory, a partially deposited one is still a candidate
                if (!candidateItems.ContainsKey(item))
                {
                    continue;
                }

                if (!ContainerHasMatchingItem(containerInventory, item))
                {
                    continue;
                }

                matchedItems.Add(item);

                if (!containerInventory.CanAddItem(item, 1))
                {
                    // no room for even a single unit, don't bother trying
                    continue;
                }

                if (!ownershipEnsured)
                {
                    if (!ContainerService.EnsureOwnership(container))
                    {
                        // someone else currently has this container open
                        return;
                    }

                    ownershipEnsured = true;
                }

                containerInventory.MoveItemToThis(playerInventory, item);
            }
        }

        private static bool ContainerHasMatchingItem(Inventory containerInventory, ItemDrop.ItemData sourceItem)
        {
            List<ItemDrop.ItemData> containerItems = containerInventory.GetAllItems();

            foreach (ItemDrop.ItemData containerItem in containerItems)
            {
                if (containerItem == null)
                {
                    continue;
                }

                if (containerItem.m_shared.m_name == sourceItem.m_shared.m_name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
