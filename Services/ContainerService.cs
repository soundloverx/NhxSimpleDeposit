using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NhxSimpleDeposit
{
    internal static class ContainerService
    {
        // Container.CheckAccess(long) is private in the actual game assembly loaded at runtime
        private static readonly MethodInfo CheckAccessMethod = AccessTools.Method(typeof(Container), "CheckAccess", new[] { typeof(long) });

        // Container.Load() is private too. It's what the game runs once a second (CheckForChanges) to refresh its
        // copy of a chest from the network data; it does nothing when that copy is current or the chest is open here.
        private static readonly MethodInfo LoadMethod = AccessTools.Method(typeof(Container), "Load", Type.EmptyTypes);

        // Inventory.Changed(bool, bool) is private; shared here so inventory mutations done by moving items
        // around directly (sorting, merging stacks) get saved and synced like the vanilla UI would
        // (a plain field/stack mutation otherwise leaves the ZDO stale).
        private static readonly MethodInfo InventoryChangedMethod = AccessTools.Method(typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) });
        private static readonly object[] InventoryChangedArguments = { false, false };

        internal static List<Container> GetNearbyContainers(Player player)
        {
            List<Container> containers = new List<Container>();

            if (player == null)
            {
                return containers;
            }

            Collider[] colliders = Physics.OverlapSphere(player.transform.position, Plugin.SearchRadius);
            HashSet<Container> foundContainers = new HashSet<Container>();

            foreach (Collider collider in colliders)
            {
                Container container = collider.GetComponentInParent<Container>();

                if (container == null)
                {
                    continue;
                }

                if (!foundContainers.Add(container))
                {
                    continue;
                }

                if (IsTombstone(container))
                {
                    continue;
                }

                // Only ever deposit into chests a player built. Dungeon and other world loot chests
                // (vanilla TreasureChest_* prefabs) carry a Piece too, just one without a creator.
                if (!IsPlayerBuilt(container))
                {
                    continue;
                }

                if (!IsAccessibleContainer(container))
                {
                    continue;
                }

                containers.Add(container);
            }

            return containers;
        }

        internal static void NotifyInventoryChanged(Inventory inventory)
        {
            InventoryChangedMethod?.Invoke(inventory, InventoryChangedArguments);
        }

        // Brings our copy of the chest up to date before writing to it. Each save overwrites the chest for everyone,
        // so saving a copy that's a moment old would undo whatever another player just put in or took out.
        internal static void Reload(Container container)
        {
            LoadMethod?.Invoke(container, null);
        }

        // Tombstones are left exactly as the death left them: never deposited into, never sorted.
        internal static bool IsTombstone(Container container)
        {
            return container.GetComponent<TombStone>() != null;
        }

        private static bool IsPlayerBuilt(Container container)
        {
            Piece piece = container.GetComponentInParent<Piece>();

            return piece != null && piece.IsPlacedByPlayer();
        }

        internal static bool IsAccessibleContainer(Container container)
        {
            if (container == null)
            {
                return false;
            }

            Piece piece = container.GetComponentInParent<Piece>();

            if (piece == null)
            {
                return false;
            }

            // A build placement ghost is a real clone of the piece prefab - Container component
            // included - but it was never registered on the network, so its ZNetView has no ZDO yet.
            // Container.CheckAccess assumes a fully-initialized container and throws on one of these.
            ZNetView nview = container.GetComponent<ZNetView>();

            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (container.m_checkGuardStone && !PrivateArea.CheckAccess(container.transform.position, 0f, false, true))
            {
                return false;
            }

            if (CheckAccessMethod == null)
            {
                return false;
            }

            long playerId = Game.instance.GetPlayerProfile().GetPlayerID();

            return (bool)CheckAccessMethod.Invoke(container, new object[] { playerId });
        }

        internal static bool EnsureOwnership(Container container)
        {
            if (container == null)
            {
                return false;
            }

            ZNetView nview = container.GetComponent<ZNetView>();

            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (nview.IsOwner())
            {
                return true;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo.GetInt(ZDOVars.s_inUse) == 1)
            {
                return false;
            }

            long previousOwner = zdo.GetOwner();

            nview.ClaimOwnership();

            if (previousOwner != 0L && ZDOMan.instance != null)
            {
                ZDOMan.instance.ForceSendZDO(previousOwner, zdo.m_uid);
            }

            return true;
        }
    }
}
