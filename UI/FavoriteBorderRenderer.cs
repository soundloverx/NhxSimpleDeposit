using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NhxSimpleDeposit.UI
{
    // Draws AzuEPI's favorite borders in the player inventory (only used without AzuEPI, which draws its own): one
    // colour for a favorited slot, one for a favorited item type, and a third for a favorited item in a favorited slot.
    // Like AzuEPI's, each border is a copy of the slot's "equip queued" overlay image, which already has the slot's size
    // and position, with a frame sprite instead.
    internal static class FavoriteBorderRenderer
    {
        private const string BorderName = "NhxSimpleDepositFavoriteBorder";

        // InventoryGrid.m_elements is private in the actual game assembly loaded at runtime
        private static readonly FieldInfo ElementsField = AccessTools.Field(typeof(InventoryGrid), "m_elements");

        private static Sprite _borderSprite;

        internal static void Refresh(InventoryGrid grid, Player player)
        {
            Inventory inventory = grid.GetInventory();

            if (player == null || inventory == null || inventory != player.GetInventory() || !FavoritesService.IsOwnUiActive)
            {
                return;
            }

            List<InventoryElement> elements = ElementsField?.GetValue(grid) as List<InventoryElement>;
            int width = inventory.GetWidth();

            if (elements == null || width <= 0)
            {
                return;
            }

            PlayerFavorites favorites = PlayerFavorites.Get(player.GetPlayerID());

            for (int index = 0; index < elements.Count; index++)
            {
                SetBorder(elements[index], favorites.IsSlotFavorited(new Vector2i(index % width, index / width)), Plugin.FavoritedSlotBorderColor);
            }

            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                int index = item.m_gridPos.y * width + item.m_gridPos.x;

                if (index < 0 || index >= elements.Count || !favorites.IsItemFavorited(item))
                {
                    continue;
                }

                Color color = favorites.IsSlotFavorited(item.m_gridPos) ? Plugin.FavoritedItemAndSlotBorderColor : Plugin.FavoritedItemBorderColor;
                SetBorder(elements[index], true, color);
            }
        }

        private static void SetBorder(InventoryElement element, bool visible, Color color)
        {
            if (element == null || element.m_queued == null)
            {
                return;
            }

            Transform existing = element.m_queued.transform.Find(BorderName);
            Image border = existing != null ? existing.GetComponent<Image>() : null;

            if (border == null)
            {
                if (!visible)
                {
                    return;
                }

                border = CreateBorder(element.m_queued);
            }

            border.color = color;
            border.enabled = visible;
        }

        // A child of the overlay, not of the slot: the game only disables the overlay's own Image when nothing is queued,
        // which leaves its children showing.
        private static Image CreateBorder(Image queued)
        {
            Image border = Object.Instantiate(queued, queued.transform);

            foreach (Transform child in border.transform)
            {
                Object.Destroy(child.gameObject);
            }

            border.name = BorderName;
            border.sprite = GetBorderSprite();
            border.raycastTarget = false;

            return border;
        }

        // A white frame with rounded corners on a transparent background, tinted by the border colour. The same shape as
        // AzuEPI's border.png (1024 px square, 24 px thick, corners rounded with a 64 px radius), at a quarter of the size.
        private static Sprite GetBorderSprite()
        {
            if (_borderSprite != null)
            {
                return _borderSprite;
            }

            const int size = 256;
            const float thickness = 6f;
            const float cornerRadius = 16f;

            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float pixelX = x + 0.5f;
                    float pixelY = y + 0.5f;
                    float outerDistance = GetRoundedSquareDistance(pixelX, pixelY, 0f, size, cornerRadius);
                    float innerDistance = GetRoundedSquareDistance(pixelX, pixelY, thickness, size - thickness, cornerRadius - thickness);

                    // how much of the pixel is inside the outer edge and outside the inner one, so the edges are anti-aliased
                    float alpha = Mathf.Clamp01(0.5f - outerDistance) * Mathf.Clamp01(0.5f + innerDistance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = BorderName,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.SetPixels32(pixels);
            texture.Apply(true, true);

            _borderSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.zero);
            _borderSprite.name = BorderName;

            return _borderSprite;
        }

        // Signed distance from a point to a square with rounded corners spanning min to max on both axes: negative inside.
        private static float GetRoundedSquareDistance(float x, float y, float min, float max, float cornerRadius)
        {
            float center = (min + max) / 2f;
            float halfStraightSide = (max - min) / 2f - cornerRadius;
            float distanceX = Mathf.Abs(x - center) - halfStraightSide;
            float distanceY = Mathf.Abs(y - center) - halfStraightSide;

            return new Vector2(Mathf.Max(distanceX, 0f), Mathf.Max(distanceY, 0f)).magnitude
                + Mathf.Min(Mathf.Max(distanceX, distanceY), 0f)
                - cornerRadius;
        }
    }
}
