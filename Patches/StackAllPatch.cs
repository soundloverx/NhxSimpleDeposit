using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NhxSimpleDeposit.Patches
{
    // Keeps favorited items out of the chest's Stack All button (and holding the use key on a chest), like AzuEPI does.
    // Only patched in when AzuEPI isn't installed, it does this itself.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll), typeof(Inventory), typeof(bool))]
    internal static class StackAllPatch
    {
        private static bool Prepare()
        {
            return FavoritesService.UseOwnFavorites;
        }

        // StackAll starts by copying the items to move: new List<ItemDrop.ItemData>(fromInventory.GetAllItems()).
        // That copy goes through RemoveFavorites before anything is moved.
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo getAllItemsMethod = AccessTools.Method(typeof(Inventory), nameof(Inventory.GetAllItems), Type.EmptyTypes);
            ConstructorInfo listConstructor = AccessTools.Constructor(typeof(List<ItemDrop.ItemData>), new[] { typeof(IEnumerable<ItemDrop.ItemData>) });
            MethodInfo removeFavoritesMethod = AccessTools.Method(typeof(StackAllPatch), nameof(RemoveFavorites));

            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, getAllItemsMethod), new CodeMatch(OpCodes.Newobj, listConstructor));

            if (matcher.IsInvalid)
            {
                Plugin.Log.LogWarning("Couldn't find where the chest Stack All button picks its items (the game may have changed): favorited items aren't protected from it.");
                return matcher.InstructionEnumeration();
            }

            return matcher
                .Advance(2)
                .Insert(new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Call, removeFavoritesMethod))
                .InstructionEnumeration();
        }

        private static List<ItemDrop.ItemData> RemoveFavorites(List<ItemDrop.ItemData> items, Inventory fromInventory)
        {
            Player player = Player.m_localPlayer;

            if (player == null || fromInventory != player.GetInventory() || !FavoritesService.IsOwnUiActive)
            {
                return items;
            }

            items.RemoveAll(item => FavoritesService.IsFavorited(player, item));
            return items;
        }
    }
}
