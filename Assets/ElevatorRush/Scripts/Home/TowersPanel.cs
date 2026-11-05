using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// The Towers panel (home plan 8): a horizontal carousel of the tower skins. Selecting one
    /// previews it live on the home tower; leaving without buying restores the equipped skin.
    /// One button buys (which equips), equips, or shows Equipped; locked skins show the level
    /// beside a gem button that unlocks and equips them now (monetization plan 2.3).
    /// </summary>
    public sealed class TowersPanel : MonoBehaviour
    {
        // Layout in reference pixels.
        static readonly Vector2 CardSize = new Vector2(250f, 330f);
        const float CardStep = 270f;
        const float CarouselTop = -10f;
        const float CarouselHeight = 350f;
        const float SwatchSize = 150f;
        const float SwatchTop = -20f;
        const float BandHeight = 20f;
        const int NameFont = 38;
        const int StatusFont = 32;
        static readonly Vector2 ActionSize = new Vector2(440f, 120f);
        static readonly Vector2 GemButtonSize = new Vector2(300f, 120f);
        const float GemButtonGap = 24f;
        const float ActionBottom = 40f;
        const int ActionFont = 54;
        const float SmallCoin = 34f;
        const float SheetCameraGap = 40f;

        struct Card
        {
            public Image back;
            public Text status;
            public Image coin;
            public Image padlock;
        }

        GameConfig config;
        Fx fx;
        Sfx sfx;
        BottomSheet sheet;
        Card[] cards;
        Button action;
        Image actionImage;
        PriceLabel actionPrice;
        Button gemButton;
        Image gemButtonImage;
        PriceLabel gemPrice;
        int selected;

        public Func<SaveData> Save { get; set; }
        public Func<string, ShopReceipt> Buy { get; set; }
        public Func<string, ShopResult> Equip { get; set; }
        /// <summary>Gem spends (early unlocks and coin shortfalls); set by GameManager.</summary>
        public GemFlow Gems { get; set; }
        /// <summary>Show this tower skin on the home tower (a preview or the equipped one), keeping the camera clear of the sheet.</summary>
        public Action<string, float> Preview { get; set; }

        public bool IsOpen => sheet.IsOpen;

        public event Action<ShopReceipt, int> Purchased;
        /// <summary>The panel closed; Home shows the equipped skin again.</summary>
        public event Action Closed;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform uiRoot, Fx fx, Sfx sfx)
        {
            this.config = config;
            this.fx = fx;
            this.sfx = sfx;
            sheet = BottomSheet.Create(uiRoot, "TowersSheet", config, font, sprites, config.towerSheetHeight, Click);
            sheet.Closed += () => Closed?.Invoke();
            var content = sheet.Content;

            // Horizontal carousel: a masked viewport scrolling a row of cards.
            var viewport = UiBuilder.Rect("Carousel", content);
            viewport.anchorMin = new Vector2(0f, 1f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.anchoredPosition = new Vector2(0f, CarouselTop);
            viewport.sizeDelta = new Vector2(0f, CarouselHeight);
            var mask = viewport.gameObject.AddComponent<Image>();
            mask.color = new Color(1f, 1f, 1f, 0.004f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var row = UiBuilder.Rect("Cards", viewport);
            row.anchorMin = row.anchorMax = new Vector2(0f, 0.5f);
            row.pivot = new Vector2(0f, 0.5f);
            row.sizeDelta = new Vector2(CardStep * TowerSkins.Count + (CardStep - CardSize.x), CarouselHeight);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = row;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.viewport = viewport;

            cards = new Card[TowerSkins.Count];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = CreateCard(row, font, sprites, i);

            action = UiBuilder.Button(content, "Action", font, sprites.Panel, "", ActionFont, config.uiPlayButton, config.uiText, config.uiShadow, Act);
            actionImage = action.GetComponent<Image>();
            UiBuilder.Place((RectTransform)action.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ActionBottom), ActionSize);
            actionPrice = new PriceLabel((RectTransform)action.transform, config, font, sprites, ActionFont, config.uiText, config.uiShadow);

            gemButton = UiBuilder.Button(content, "UnlockNow", font, sprites.Panel, "", ActionFont, config.uiGemButton, config.uiText, config.uiShadow, UnlockSelected);
            gemButtonImage = gemButton.GetComponent<Image>();
            float pairWidth = ActionSize.x + GemButtonGap + GemButtonSize.x;
            UiBuilder.Place((RectTransform)gemButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(pairWidth * 0.5f - GemButtonSize.x * 0.5f, ActionBottom), GemButtonSize);
            gemPrice = new PriceLabel((RectTransform)gemButton.transform, config, font, sprites, ActionFont, config.uiText, config.uiShadow);
        }

        Card CreateCard(RectTransform row, Font font, SpriteFactory sprites, int index)
        {
            var skin = TowerSkins.At(index);
            var item = Catalog.Towers[index];
            var button = UiBuilder.Button(row, item.name, font, sprites.Panel, "", 1, Color.white, Color.clear, Color.clear, () => { Click(); Select(index); });
            var rt = UiBuilder.Place((RectTransform)button.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2((CardStep - CardSize.x) + index * CardStep, 0f), CardSize);
            var card = new Card { back = button.GetComponent<Image>() };

            // A swatch of the skin: exterior with the trim, slab and shaft as bands, and a tower icon.
            var top = new Vector2(0.5f, 1f);
            var swatch = UiBuilder.Image(rt, "Exterior", sprites.Panel, skin.exterior, true);
            UiBuilder.Place(swatch.rectTransform, top, top, new Vector2(0f, SwatchTop), Vector2.one * SwatchSize);
            var bands = new[] { skin.trim, skin.slab, skin.shaft };
            for (int b = 0; b < bands.Length; b++)
            {
                var band = UiBuilder.Image(swatch.transform, "Band" + b, null, bands[b], false);
                UiBuilder.Place(band.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, b * BandHeight), new Vector2(SwatchSize, BandHeight));
            }
            var icon = UiBuilder.Image(swatch.transform, "Tower", sprites.Tower, skin.ornamentMain, false);
            UiBuilder.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), Vector2.one * SwatchSize * 0.5f);

            var name = UiBuilder.Label(rt, "Name", font, item.name, NameFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = NameFont / 2;
            name.resizeTextMaxSize = NameFont;
            UiBuilder.Place(name.rectTransform, top, new Vector2(0.5f, 0.5f), new Vector2(0f, SwatchTop - SwatchSize - 40f), new Vector2(CardSize.x - 20f, NameFont * 1.4f));

            card.status = UiBuilder.Label(rt, "Status", font, "", StatusFont, config.uiNextCircle, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(card.status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(SmallCoin * 0.5f, 34f), new Vector2(CardSize.x, StatusFont));
            card.coin = UiBuilder.Image(rt, "Coin", sprites.Coin, config.uiCoin, false);
            UiBuilder.Place(card.coin.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(-30f, 34f), Vector2.one * SmallCoin);
            card.padlock = UiBuilder.Image(swatch.transform, "Lock", sprites.Lock, config.uiText, false);
            UiBuilder.Place(card.padlock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * SwatchSize * 0.45f);
            return card;
        }

        void Click()
        {
            sfx.Play(SfxKey.Button);
        }

        public void Open()
        {
            var save = Save();
            selected = Index(save.equippedTower);
            sheet.Open("Towers");
            Refresh();
            ShowSelected();
        }

        public void Close()
        {
            sheet.Hide();
        }

        static int Index(string id)
        {
            for (int i = 0; i < Catalog.Towers.Length; i++)
                if (Catalog.Towers[i].id == id)
                    return i;
            return 0;
        }

        void Select(int index)
        {
            selected = index;
            Refresh();
            ShowSelected();
        }

        /// <summary>Live preview of the selected skin, locked or not, on the home tower.</summary>
        void ShowSelected()
        {
            Preview(Catalog.Towers[selected].id, sheet.Height + SheetCameraGap);
        }

        public void Refresh()
        {
            var save = Save();
            for (int i = 0; i < cards.Length; i++)
            {
                var item = Catalog.Towers[i];
                ref var card = ref cards[i];
                bool owned = save.Owns(item.id);
                bool unlocked = UnlockSchedule.IsUnlocked(item.id, save);
                card.back.color = i == selected ? config.uiStarOn : Color.white;
                card.padlock.gameObject.SetActive(!unlocked);
                card.coin.gameObject.SetActive(!owned && unlocked);
                card.status.text = item.id == save.equippedTower ? "Equipped" : owned ? "Owned" : unlocked ? item.price.ToString() : "Level " + item.unlockAt;
            }

            var current = Catalog.Towers[selected];
            bool locked = !save.Owns(current.id) && !UnlockSchedule.IsUnlocked(current.id, save);
            ShowGemButton(locked);
            if (current.id == save.equippedTower)
            {
                actionPrice.ShowText("Equipped");
                SetAction(config.uiEquipped, false);
            }
            else if (save.Owns(current.id))
            {
                actionPrice.ShowText("Equip");
                SetAction(config.uiOwned, true);
            }
            else if (locked)
            {
                // The level it unlocks at, beside a gem button to have it now.
                int gems = GemMath.EarlyUnlockGems(current.price);
                actionPrice.ShowLocked("Level " + current.unlockAt);
                SetAction(config.uiCantAfford, false);
                gemPrice.ShowGems(gems);
                gemButtonImage.color = save.gems >= gems ? config.uiGemButton : config.uiCantAfford;
            }
            else if (save.coins >= current.price)
            {
                actionPrice.ShowCoins(current.price);
                SetAction(config.uiPlayButton, true);
            }
            else
            {
                int gems = GemMath.ShortfallGems(current.price, save.coins);
                actionPrice.ShowCoinsAndGems(save.coins, gems);
                SetAction(save.gems >= gems ? config.uiGemButton : config.uiCantAfford, true);
            }
        }

        void SetAction(Color tint, bool interactable)
        {
            actionImage.color = tint;
            action.interactable = interactable;
        }

        /// <summary>The gem button shows beside the action only for a locked skin, which moves the action left.</summary>
        void ShowGemButton(bool visible)
        {
            gemButton.gameObject.SetActive(visible);
            float pairWidth = ActionSize.x + GemButtonGap + GemButtonSize.x;
            var rt = (RectTransform)action.transform;
            rt.anchoredPosition = new Vector2(visible ? ActionSize.x * 0.5f - pairWidth * 0.5f : 0f, ActionBottom);
        }

        void UnlockSelected()
        {
            int before = Save().coins;
            Gems.UnlockEarly(Catalog.Towers[selected].id, r => Bought(r, before));
        }

        /// <summary>Bought or unlocked: the skin is now owned and equipped.</summary>
        void Bought(ShopReceipt receipt, int coinsBefore)
        {
            sfx.Play(SfxKey.Buy);
            Fx.Haptic();
            fx.Confetti();
            Purchased?.Invoke(receipt, coinsBefore);
            Refresh();
        }

        void Act()
        {
            var save = Save();
            var current = Catalog.Towers[selected];
            if (save.Owns(current.id))
            {
                Click();
                Equip(current.id);
            }
            else
            {
                int before = save.coins;
                var receipt = Buy(current.id);
                if (receipt.result == ShopResult.NotEnoughCoins)
                {
                    Gems.BuyWithGems(current.id, r => Bought(r, before));
                    return;
                }
                if (!receipt.Ok)
                {
                    sfx.Play(SfxKey.Angry);
                    fx.Punch(action.transform, config.uiPunch);
                    return;
                }
                Bought(receipt, before);
                return;
            }
            Refresh();
        }
    }
}
