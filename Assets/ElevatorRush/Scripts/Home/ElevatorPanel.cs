using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// The Elevator panel (home plan 6): one row per upgrade with its icon, name, tier pips, the
    /// next effect in plain words, and a price button, "Max", or a lock with the level needed.
    /// Tiers are bought in order and only up to the current tier cap; a tier above the cap can
    /// be bought now with gems (monetization plan 2.3), its level shown above the gem button.
    /// </summary>
    public sealed class ElevatorPanel : MonoBehaviour
    {
        // Layout in reference pixels.
        const float RowWidth = 1000f;
        const float RowHeight = 180f;
        const float RowStep = 196f;
        const float RowTop = -10f;
        const float IconSize = 120f;
        const float Padding = 24f;
        const float PipSize = 30f;
        const float PipStep = 38f;
        static readonly Vector2 ButtonSize = new Vector2(250f, 110f);
        const int NameFont = 52;
        const int CaptionFont = 30;
        const float CaptionHeight = 34f;
        const int EffectFont = 36;
        const int ButtonFont = 46;

        struct Row
        {
            public RectTransform root;
            public Image[] pips;
            public Text effect;
            public Button button;
            public Image buttonImage;
            public PriceLabel price;
            public PriceLabel caption;
        }

        GameConfig config;
        Fx fx;
        Sfx sfx;
        BottomSheet sheet;
        Row[] rows;

        public Func<SaveData> Save { get; set; }
        public Func<string, ShopReceipt> Buy { get; set; }
        /// <summary>Gem spends (early tiers and coin shortfalls); set by GameManager.</summary>
        public GemFlow Gems { get; set; }

        public bool IsOpen => sheet.IsOpen;

        /// <summary>A tier was bought: the receipt, the balance before it, and which upgrade.</summary>
        public event Action<ShopReceipt, int, UpgradeDef> Purchased;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform uiRoot, Fx fx, Sfx sfx)
        {
            this.config = config;
            this.fx = fx;
            this.sfx = sfx;
            sheet = BottomSheet.Create(uiRoot, "ElevatorSheet", config, font, sprites, config.upgradeSheetHeight, Click);

            var icons = new[] { sprites.ArrowUp, sprites.Swap, sprites.Heart, sprites.Coin, sprites.Person };
            var colors = new[] { config.uiNextButton, config.uiOwned, config.uiHeart, config.uiCoin, config.uiLevelCircle };
            rows = new Row[Catalog.Upgrades.Length];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = CreateRow(sheet.Content, font, sprites, Catalog.Upgrades[i], icons[i], colors[i], i);
        }

        Row CreateRow(RectTransform parent, Font font, SpriteFactory sprites, UpgradeDef upgrade, Sprite icon, Color iconColor, int index)
        {
            var row = new Row();
            var bg = UiBuilder.Image(parent, upgrade.name, sprites.Panel, Color.white, true);
            row.root = UiBuilder.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, RowTop - index * RowStep), new Vector2(RowWidth, RowHeight));

            var left = new Vector2(0f, 0.5f);
            var circle = UiBuilder.Image(row.root, "IconBack", sprites.Circle, iconColor, false);
            UiBuilder.Place(circle.rectTransform, left, left, new Vector2(Padding, 0f), Vector2.one * IconSize);
            var image = UiBuilder.Image(circle.transform, "Icon", icon, config.uiText, false);
            UiBuilder.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * IconSize * 0.62f);

            float textX = Padding * 2f + IconSize;
            var name = UiBuilder.Label(row.root, "Name", font, upgrade.name, NameFont, config.uiTextDark, TextAnchor.MiddleLeft, Color.clear);
            UiBuilder.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(textX, -RowHeight * 0.27f), new Vector2(300f, NameFont));

            row.pips = new Image[upgrade.MaxTier];
            for (int t = 0; t < row.pips.Length; t++)
            {
                row.pips[t] = UiBuilder.Image(row.root, "Pip" + t, sprites.Circle, config.uiStarOff, false);
                UiBuilder.Place(row.pips[t].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                    new Vector2(textX + 260f + t * PipStep, -RowHeight * 0.27f), Vector2.one * PipSize);
            }

            row.effect = UiBuilder.Label(row.root, "Effect", font, "", EffectFont, config.uiNextCircle, TextAnchor.MiddleLeft, Color.clear);
            UiBuilder.Place(row.effect.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(textX, RowHeight * 0.3f), new Vector2(560f, EffectFont));

            row.button = UiBuilder.Button(row.root, "Buy", font, sprites.Panel, "", ButtonFont, config.uiPlayButton, config.uiText, config.uiShadow,
                () => Choose(upgrade, index));
            row.buttonImage = row.button.GetComponent<Image>();
            UiBuilder.Place((RectTransform)row.button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-Padding, 0f), ButtonSize);
            row.price = new PriceLabel((RectTransform)row.button.transform, config, font, sprites, ButtonFont, config.uiText, config.uiShadow);
            var caption = UiBuilder.Place(UiBuilder.Rect("Caption", row.root), Vector2.one, Vector2.one, new Vector2(-Padding, -2f),
                new Vector2(ButtonSize.x, CaptionHeight));
            row.caption = new PriceLabel(caption, config, font, sprites, CaptionFont, config.uiNextCircle, Color.clear);
            return row;
        }

        void Click()
        {
            sfx.Play(SfxKey.Button);
        }

        public void Open()
        {
            sheet.Open("Elevator");
            Refresh();
        }

        public void Close()
        {
            sheet.Hide();
        }

        public void Refresh()
        {
            var save = Save();
            int done = save.LevelsCompleted;
            for (int i = 0; i < rows.Length; i++)
            {
                var upgrade = Catalog.Upgrades[i];
                ref var row = ref rows[i];
                int tier = save.UpgradeTier(upgrade.id);
                for (int t = 0; t < row.pips.Length; t++)
                    row.pips[t].color = t < tier ? config.uiCoin : config.uiStarOff;

                bool maxed = tier >= upgrade.MaxTier;
                row.effect.text = maxed ? "Max: " + EffectText(upgrade, tier) : "Next: " + EffectText(upgrade, 1);
                row.caption.SetVisible(false);
                if (maxed)
                {
                    row.price.ShowText("Max");
                    SetButton(ref row, config.uiEquipped, false);
                    continue;
                }
                int next = tier + 1;
                int price = upgrade.PriceOf(next);
                if (!UnlockSchedule.IsTierUnlocked(upgrade, next, done))
                {
                    // Above the cap: the level it opens at, and a gem button to have it now.
                    int gems = GemMath.EarlyUnlockGems(upgrade, next);
                    row.caption.ShowLocked("Level " + UnlockSchedule.UnlockAt(upgrade, next));
                    row.price.ShowGems(gems);
                    SetButton(ref row, save.gems >= gems ? config.uiGemButton : config.uiCantAfford, true);
                    continue;
                }
                if (save.coins >= price)
                {
                    row.price.ShowCoins(price);
                    SetButton(ref row, config.uiPlayButton, true);
                }
                else
                {
                    int gems = GemMath.ShortfallGems(price, save.coins);
                    row.price.ShowCoinsAndGems(save.coins, gems);
                    SetButton(ref row, save.gems >= gems ? config.uiGemButton : config.uiCantAfford, true);
                }
            }
        }

        /// <summary>The effect of this many tiers in plain words, e.g. "Doors open 16% faster".</summary>
        public static string EffectText(UpgradeDef upgrade, int tiers)
        {
            int amount = upgrade.kind == UpgradeKind.Capacity ? tiers : UpgradeMath.PercentPerTier(upgrade.kind) * tiers;
            string text = string.Format(upgrade.effectText, amount);
            return upgrade.kind == UpgradeKind.Capacity && amount > 1 ? text + "s" : text;
        }

        static void SetButton(ref Row row, Color color, bool interactable)
        {
            row.buttonImage.color = color;
            row.button.interactable = interactable;
        }

        void Choose(UpgradeDef upgrade, int index)
        {
            var save = Save();
            int before = save.coins;
            int next = save.UpgradeTier(upgrade.id) + 1;
            if (next <= upgrade.MaxTier && !UnlockSchedule.IsTierUnlocked(upgrade, next, save.LevelsCompleted))
            {
                Gems.UnlockEarly(upgrade.id, r => Bought(r, before, upgrade, index));
                return;
            }
            var receipt = Buy(upgrade.id);
            if (receipt.result == ShopResult.NotEnoughCoins)
            {
                Gems.BuyWithGems(upgrade.id, r => Bought(r, before, upgrade, index));
                return;
            }
            if (!receipt.Ok)
            {
                sfx.Play(SfxKey.Angry);
                fx.Punch(rows[index].root, config.uiPunch);
                return;
            }
            Bought(receipt, before, upgrade, index);
        }

        void Bought(ShopReceipt receipt, int coinsBefore, UpgradeDef upgrade, int index)
        {
            sfx.Play(SfxKey.Buy);
            Fx.Haptic();
            var rt = (RectTransform)rows[index].button.transform;
            fx.ConfettiBurst(rt.position, config.purchaseConfetti, config.purchaseConfettiSpeed);
            fx.Punch(rows[index].pips[Save().UpgradeTier(upgrade.id) - 1].rectTransform, config.uiPunch * 2f);
            Purchased?.Invoke(receipt, coinsBefore, upgrade);
            Refresh();
        }
    }
}
