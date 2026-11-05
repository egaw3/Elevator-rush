using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// The "Next unlock" teaser (home plan 4.2), shown on Home and on LevelComplete: an icon of the
    /// next item, its name, and a bar filling from the previous threshold to the next. After the
    /// last unlock it shows tower progress instead. Not a MonoBehaviour; it only owns UI elements.
    /// </summary>
    public sealed class UnlockTeaser
    {
        // Layout in reference pixels, relative to the teaser's own rect.
        const float IconSize = 110f;
        const float Padding = 22f;
        const float BarHeight = 34f;
        const float BarInset = 6f;
        const int TitleFont = 48;
        const int SubFont = 38;

        readonly GameConfig config;
        readonly SpriteFactory sprites;
        readonly RectTransform root;
        readonly Image icon;
        readonly Text title;
        readonly Text sub;
        readonly RectTransform fill;

        public RectTransform Root => root;

        public UnlockTeaser(RectTransform parent, GameConfig config, Font font, SpriteFactory sprites, Color background, Color textColor, Vector2 size)
        {
            this.config = config;
            this.sprites = sprites;
            var bg = UiBuilder.Image(parent, "NextUnlock", sprites.Panel, background, true);
            root = bg.rectTransform;
            root.sizeDelta = size;

            var left = new Vector2(0f, 0.5f);
            icon = UiBuilder.Image(root, "Icon", sprites.Circle, Color.white, false);
            UiBuilder.Place(icon.rectTransform, left, left, new Vector2(Padding, 0f), Vector2.one * IconSize);

            float textX = Padding * 2f + IconSize;
            float textWidth = size.x - textX - Padding;
            title = UiBuilder.Label(root, "Title", font, "", TitleFont, textColor, TextAnchor.MiddleLeft, Color.clear);
            UiBuilder.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(textX, -Padding * 0.6f), new Vector2(textWidth, TitleFont * 1.2f));

            var barBack = UiBuilder.Image(root, "Bar", sprites.Panel, config.uiBarBack, true);
            UiBuilder.Place(barBack.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(textX, Padding), new Vector2(textWidth, BarHeight));
            var fillArea = UiBuilder.Stretch(UiBuilder.Rect("FillArea", barBack.transform));
            fillArea.offsetMin = Vector2.one * BarInset;
            fillArea.offsetMax = -Vector2.one * BarInset;
            fill = UiBuilder.Stretch(UiBuilder.Image(fillArea, "Fill", sprites.Panel, config.uiBarFill, true).rectTransform);

            sub = UiBuilder.Label(barBack.transform, "Sub", font, "", SubFont, config.uiText, TextAnchor.MiddleCenter, config.uiShadow);
            UiBuilder.Stretch(sub.rectTransform);
        }

        /// <summary>Shows the next unlock at this many levels completed, or tower progress after the last one.</summary>
        public void Show(int levelsCompleted, float towerFraction)
        {
            var next = UnlockSchedule.Next(levelsCompleted);
            float f;
            if (next.IsDone)
            {
                icon.sprite = sprites.Tower;
                icon.color = config.uiCoin;
                int percent = Mathf.FloorToInt(towerFraction * 100f);
                title.text = "Tower " + percent + "%";
                sub.text = percent >= 100 ? "Complete!" : "Keep building";
                f = towerFraction;
            }
            else
            {
                icon.sprite = IconOf(sprites, next.id);
                icon.color = ColorOf(config, next.id);
                title.text = "Next: " + LabelOf(next.id);
                sub.text = next.levelsToGo == 1 ? "1 level to go" : next.levelsToGo + " levels to go";
                f = next.fill;
            }
            f = Mathf.Clamp01(f);
            fill.anchorMax = new Vector2(f, 1f);
            fill.gameObject.SetActive(f > 0f);
        }

        // ---- Shared look of an unlock id (also for the "Unlocked!" popup later) ----

        public static Sprite IconOf(SpriteFactory sprites, string id)
        {
            var item = Catalog.Find(id);
            if (item == null)
                return sprites.Star;
            switch (item.kind)
            {
                case ItemKind.Floor: return sprites.IconSprite(FloorPalette.IconOf(item.floor));
                case ItemKind.Skin: return sprites.Person;
                case ItemKind.Tower: return sprites.Tower;
                case ItemKind.ElevatorTier: return sprites.ArrowUp;
                default: return sprites.House;
            }
        }

        public static Color ColorOf(GameConfig config, string id)
        {
            var item = Catalog.Find(id);
            if (item == null)
                return config.uiStarOn;
            switch (item.kind)
            {
                case ItemKind.Floor: return FloorPalette.ColorOf(item.floor);
                case ItemKind.Skin: return config.uiLevelCircle;
                case ItemKind.Tower: return config.uiCoin;
                default: return config.uiNextButton;
            }
        }

        public static string LabelOf(string id)
        {
            var item = Catalog.Find(id);
            if (item == null)
                return Catalog.NameOf(id);
            switch (item.kind)
            {
                case ItemKind.Floor: return "Floor " + item.floor + ", " + item.name;
                case ItemKind.Skin: return item.name + " skin";
                case ItemKind.Tower: return item.name + " tower";
                default: return item.name;
            }
        }
    }
}
