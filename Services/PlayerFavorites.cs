using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NhxSimpleDeposit
{
    // This mod's own favorites, used when AzuExtendedPlayerInventory isn't installed. They're a copy of AzuEPI's
    // (AzuEPI.Game.Favoriting.UserConfig), down to the file: the same BepInEx\config\AzuExtendedPlayerInventory_player_<id>.dat
    // in the same format, so favorites carry over both ways when AzuEPI is installed or removed. Like AzuEPI, AzuAutoStore's
    // copy of the favorites is merged in when they're loaded and kept in sync when they're saved, if that file exists.
    //
    // NhxHotbarSwap moves slot favorites along with their items when it swaps the hotbar rows, reaching Get, _favoritedSlots
    // and Save through reflection (as it does with AzuEPI's GetPlayerConfig, _favoritedSlots and Save), so keep those names.
    internal class PlayerFavorites
    {
        private static readonly Dictionary<long, PlayerFavorites> FavoritesByPlayerId = new Dictionary<long, PlayerFavorites>();
        private static readonly BinaryFormatter Formatter = new BinaryFormatter();

        private readonly string _path;
        private readonly string _mirrorPath;
        private readonly HashSet<Vector2i> _favoritedSlots = new HashSet<Vector2i>();
        private readonly HashSet<string> _favoritedItems = new HashSet<string>();

        // A favorites file exists but couldn't be read at all. Everything counts as favorited then and nothing is saved
        // (which would overwrite the favorites that couldn't be read), the same stance as when AzuEPI's can't be read.
        internal bool IsUnreadable { get; private set; }

        internal static PlayerFavorites Get(long playerId)
        {
            if (!FavoritesByPlayerId.TryGetValue(playerId, out PlayerFavorites favorites))
            {
                favorites = new PlayerFavorites(playerId);
                FavoritesByPlayerId[playerId] = favorites;
            }

            return favorites;
        }

        private PlayerFavorites(long playerId)
        {
            _path = Path.Combine(Paths.ConfigPath, $"AzuExtendedPlayerInventory_player_{playerId}.dat");
            _mirrorPath = Path.Combine(Paths.ConfigPath, $"AzuAutoStore_player_{playerId}.dat");

            // Merged, not one or the other: a lost favorite gets items stored away, an extra one doesn't hurt
            Load(_path);
            Load(_mirrorPath);
        }

        internal bool IsSlotFavorited(Vector2i position)
        {
            return IsUnreadable || _favoritedSlots.Contains(position);
        }

        internal bool IsItemFavorited(ItemDrop.ItemData item)
        {
            return IsUnreadable || _favoritedItems.Contains(item.m_shared.m_name);
        }

        internal bool IsItemOrSlotFavorited(ItemDrop.ItemData item)
        {
            return IsItemFavorited(item) || IsSlotFavorited(item.m_gridPos);
        }

        internal void ToggleSlot(Vector2i position)
        {
            if (!_favoritedSlots.Remove(position))
            {
                _favoritedSlots.Add(position);
            }

            Save();
        }

        // An item favorite covers its whole item type, every stack of it wherever it sits
        internal void ToggleItem(ItemDrop.ItemData item)
        {
            string itemName = item.m_shared.m_name;

            if (!_favoritedItems.Remove(itemName))
            {
                _favoritedItems.Add(itemName);
            }

            Save();
        }

        private void Load(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                using (Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // The slots as (x, y) tuples, then the item names. Like AzuEPI, a part that isn't there or can't
                    // be parsed counts as empty.
                    if (TryDeserialize(stream) is List<Tuple<int, int>> slots)
                    {
                        foreach (Tuple<int, int> slot in slots)
                        {
                            _favoritedSlots.Add(new Vector2i(slot.Item1, slot.Item2));
                        }
                    }

                    if (TryDeserialize(stream) is List<string> itemNames)
                    {
                        _favoritedItems.UnionWith(itemNames);
                    }
                }
            }
            catch (Exception exception)
            {
                IsUnreadable = true;
                Plugin.Log.LogError($"Couldn't read the favorites in {path}: {exception.Message}. Quick stack is disabled until the game is restarted, so favorited items aren't moved.");
            }
        }

        private static object TryDeserialize(Stream stream)
        {
            try
            {
                return Formatter.Deserialize(stream);
            }
            catch (SerializationException)
            {
                return null;
            }
        }

        private void Save()
        {
            if (IsUnreadable)
            {
                Plugin.Log.LogWarning("Favorites not saved: the favorites file couldn't be read when the game started.");
                return;
            }

            Write(_path);

            if (File.Exists(_mirrorPath))
            {
                Write(_mirrorPath);
            }
        }

        private void Write(string path)
        {
            try
            {
                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    Formatter.Serialize(stream, _favoritedSlots.Select(slot => Tuple.Create(slot.x, slot.y)).ToList());
                    Formatter.Serialize(stream, _favoritedItems.ToList());
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Plugin.Log.LogWarning($"Couldn't save the favorites to {path}: {exception.Message}");
            }
        }
    }
}
