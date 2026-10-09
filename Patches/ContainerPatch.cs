using HarmonyLib;

namespace NhxSimpleDeposit.Patches
{
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
    internal static class ContainerPatch
    {
        // The reload runs before the chest is shown, and before other mods' prefixes: anything they add to the chest
        // on open (Jewelcrafting puts a Divinity Orb in world treasure chests) would otherwise be thrown away by it.
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Container __instance, bool granted)
        {
            if (!granted || !InventorySortService.CanSort(__instance))
            {
                return;
            }

            ContainerService.Reload(__instance);
        }

        private static void Postfix(Container __instance, bool granted)
        {
            if (!granted)
            {
                return;
            }

            InventorySortService.SortContainer(__instance);
        }
    }
}
