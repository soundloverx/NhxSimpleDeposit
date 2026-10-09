using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NhxSimpleDeposit
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Optional: when installed, its favorites protect items from quick stack (see AzuEpiService), otherwise our own copy
    // of them does (see FavoritesService). Loading after it is also what lets Awake tell whether it's there.
    [BepInDependency(AzuEpiService.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
    // NhxQualityPack contains these same features (same hotkey, same chest sorting), so running both would do everything twice
    [BepInIncompatibility("NhxQualityPack")]
    public class Plugin : BaseUnityPlugin
    {
        private const string PluginGuid = "NhxSimpleDeposit";
        private const string PluginName = "NhxSimpleDeposit";
        private const string PluginVersion = "1.0.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        // AzuEPI's defaults for its favoriting settings
        private static readonly KeyboardShortcut DefaultFavoritingModifierKey = new KeyboardShortcut(KeyCode.LeftAlt);
        private static readonly Color DefaultFavoritedItemBorderColor = new Color(1f, 0.8482759f, 0f);
        private static readonly Color DefaultFavoritedItemAndSlotBorderColor = new Color(0.5f, 0.67413795f, 0.5f);
        private static readonly Color DefaultFavoritedSlotBorderColor = new Color(0f, 0.5f, 1f);

        private ConfigEntry<int> _searchRadius;
        private ConfigEntry<KeyboardShortcut> _quickStackShortcut;
        private ConfigEntry<KeyboardShortcut> _favoritingModifierKey;
        private ConfigEntry<Color> _favoritedItemBorderColor;
        private ConfigEntry<Color> _favoritedItemAndSlotBorderColor;
        private ConfigEntry<Color> _favoritedSlotBorderColor;
        private ConfigEntry<bool> _showFavoriteTooltip;
        private Harmony _harmonyInstance;

        internal static int SearchRadius => Instance?._searchRadius != null ? Instance._searchRadius.Value : 25;
        internal static KeyboardShortcut QuickStackShortcut => Instance?._quickStackShortcut != null ? Instance._quickStackShortcut.Value : new KeyboardShortcut(KeyCode.P);
        internal static KeyboardShortcut FavoritingModifierKey => Instance?._favoritingModifierKey != null ? Instance._favoritingModifierKey.Value : DefaultFavoritingModifierKey;
        internal static Color FavoritedItemBorderColor => Instance?._favoritedItemBorderColor != null ? Instance._favoritedItemBorderColor.Value : DefaultFavoritedItemBorderColor;
        internal static Color FavoritedItemAndSlotBorderColor => Instance?._favoritedItemAndSlotBorderColor != null ? Instance._favoritedItemAndSlotBorderColor.Value : DefaultFavoritedItemAndSlotBorderColor;
        internal static Color FavoritedSlotBorderColor => Instance?._favoritedSlotBorderColor != null ? Instance._favoritedSlotBorderColor.Value : DefaultFavoritedSlotBorderColor;
        internal static bool ShowFavoriteTooltip => Instance?._showFavoriteTooltip == null || Instance._showFavoriteTooltip.Value;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            _searchRadius = Config.Bind(
                "General",
                "Search Radius",
                25,
                new ConfigDescription(
                    "The radius in which to search for nearby containers when quick stacking.",
                    new AcceptableValueRange<int>(5, 150)
                )
            );

            _quickStackShortcut = Config.Bind(
                "General",
                "Quick Stack Shortcut",
                new KeyboardShortcut(KeyCode.P),
                "Keyboard shortcut used to quick stack nearby containers."
            );

            // With AzuEPI installed its own favoriting settings apply, so these aren't even shown then
            if (FavoritesService.UseOwnFavorites)
            {
                BindFavoritesConfig();
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(assembly);

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void BindFavoritesConfig()
        {
            const string section = "Favorites";

            _favoritingModifierKey = Config.Bind(
                section,
                "Favoriting Modifier Key",
                DefaultFavoritingModifierKey,
                "Hold this while left-clicking an item to favorite its item type, or while right-clicking any slot of your inventory to favorite the slot. Favorites are never quick stacked. Only used without AzuExtendedPlayerInventory: with it, its own favorites are used."
            );

            _favoritedItemBorderColor = Config.Bind(section, "Favorited Item Border Color", DefaultFavoritedItemBorderColor, "Color of the border around favorited items.");
            _favoritedItemAndSlotBorderColor = Config.Bind(section, "Favorited Item and Slot Border Color", DefaultFavoritedItemAndSlotBorderColor, "Color of the border when both the item and its slot are favorited.");
            _favoritedSlotBorderColor = Config.Bind(section, "Favorited Slot Border Color", DefaultFavoritedSlotBorderColor, "Color of the border around favorited slots.");
            _showFavoriteTooltip = Config.Bind(section, "Display Tooltip Hint", true, "Show favoriting status in item tooltips.");
        }

        private void Update()
        {
            HandleQuickStack();
        }

        private static void HandleQuickStack()
        {
            if (!InputService.IsDown(QuickStackShortcut))
            {
                return;
            }

            if (IsTypingInInputField())
            {
                Log.LogInfo("Quick stack hotkey ignored: an input field is focused.");
                return;
            }

            Player player = Player.m_localPlayer;

            if (player == null)
            {
                return;
            }

            QuickStackResult result = QuickStackService.QuickStack(player);
            Log.LogInfo($"Quick stack: {result.Outcome} ({result.Deposited}/{result.TotalEligible}).");
            ShowQuickStackResultMessage(player, result);
        }

        private static void ShowQuickStackResultMessage(Player player, QuickStackResult result)
        {
            switch (result.Outcome)
            {
                case QuickStackOutcome.NoContainersFound:
                    player.Message(MessageHud.MessageType.Center, "No available containers");
                    break;

                case QuickStackOutcome.NothingToDeposit:
                    break;

                case QuickStackOutcome.NoMatchingItems:
                    player.Message(MessageHud.MessageType.Center, "No matching containers");
                    break;

                case QuickStackOutcome.NoRoomForItems:
                    player.Message(MessageHud.MessageType.Center, "No room for items");
                    break;

                case QuickStackOutcome.FavoritesUnavailable:
                    player.Message(MessageHud.MessageType.Center, FavoritesService.UnavailableMessage);
                    break;

                case QuickStackOutcome.ItemsDeposited:
                    if (result.Deposited >= result.TotalEligible)
                    {
                        player.Message(MessageHud.MessageType.Center, "All items deposited");
                    }
                    else
                    {
                        player.Message(MessageHud.MessageType.Center, $"{result.Deposited}/{result.TotalEligible} items deposited");
                    }
                    break;
            }
        }

        private void OnDestroy()
        {
            _harmonyInstance?.UnpatchSelf();

            Log?.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            Instance = null;
        }

        private static bool IsTypingInInputField()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            {
                return false;
            }

            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

            // The EventSystem keeps pointing at an input field after its panel is closed (e.g. the portal/sign
            // text dialog only deactivates its panel), so being selected isn't enough - it has to be focused.
            InputField legacyField = selectedObject.GetComponent<InputField>();
            TMP_InputField tmpField = selectedObject.GetComponent<TMP_InputField>();

            return (legacyField != null && legacyField.isFocused) || (tmpField != null && tmpField.isFocused);
        }
    }
}
