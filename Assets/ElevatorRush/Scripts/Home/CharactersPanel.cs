using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// The Characters panel (home plan 7): a row of the 8 destination-color chips (locked ones
    /// show a lock and the level), a grid of skins, and a live preview of the selected color
    /// wearing the selected skin on the ground floor, with the camera moved in. One button
    /// buys (and equips), equips, or shows Equipped. A locked skin shows its level beside a gem
    /// button that unlocks it now; tapping the next locked color offers its floor for gems
    /// (monetization plan 2.3).
    /// </summary>
    public sealed class CharactersPanel : MonoBehaviour
    {
        // Layout in reference pixels.
        const float ChipSize = 100f;
        const float ChipStep = 122f;
        const float ChipY = -20f;
        const float ChipRing = 12f;
        const int ChipLevelFont = 28;
        const int Columns = 4;
        static readonly Vector2 CardSize = new Vector2(232f, 150f);
        const float CardStepX = 248f;
        const float CardStepY = 164f;
        const float GridTop = -190f;
        const float CardIconSize = 64f;
        const int CardNameFont = 34;
        const int CardStatusFont = 30;
        static readonly Vector2 ActionSize = new Vector2(440f, 120f);
        static readonly Vector2 GemButtonSize = new Vector2(300f, 120f);
        const float GemButtonGap = 24f;
        const float ActionBottom = 40f;
        const int ActionFont = 54;
        const float SmallCoin = 32f;
        const float SheetCameraGap = 40f;
        // Where the preview passenger stands, as a fraction of the room width on the ground floor.
        const float PreviewX = 0.5f;

        struct Chip
        {
            public Image ring;
            public Image fill;
            public Image icon;
            public Image padlock;
            public Text level;
        }

        struct Card
        {
            public Image back;
            public Image icon;
            public Text name;
            public Text status;
            public Image coin;
        }

        GameConfig config;
        Fx fx;
        Sfx sfx;
        SpriteFactory sprites;
        Building building;
        HomeController home;
        PassengerSpawner spawner;
        BottomSheet sheet;
        Chip[] chips;
        Card[] cards;
        Button action;
        Image actionImage;
        PriceLabel actionPrice;
        Button gemButton;
        Image gemButtonImage;
        PriceLabel gemPrice;

        int color;
        int skin;

        public Func<SaveData> Save { get; set; }
        public Func<string, ShopReceipt> Buy { get; set; }
        public Func<string, int, ShopResult> Equip { get; set; }
        /// <summary>Gem spends (early unlocks and coin shortfalls); set by GameManager.</summary>
        public GemFlow Gems { get; set; }

        public bool IsOpen => sheet.IsOpen;

        /// <summary>A skin was bought: the receipt and the balance before it.</summary>
        public event Action<ShopReceipt, int> Purchased;
        /// <summary>The panel closed; Home puts the camera back.</summary>
        public event Action Closed;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, RectTransform uiRoot, Building building,
            HomeController home, PassengerSpawner spawner, Fx fx, Sfx sfx)
        {
            this.config = config;
            this.sprites = sprites;
            this.building = building;
            this.home = home;
            this.spawner = spawner;
            this.fx = fx;
            this.sfx = sfx;
            sheet = BottomSheet.Create(uiRoot, "CharactersSheet", config, font, sprites, config.skinSheetHeight, Click);
            sheet.Closed += OnSheetClosed;
            var content = sheet.Content;
            var top = new Vector2(0.5f, 1f);
            var center = new Vector2(0.5f, 0.5f);

            chips = new Chip[Catalog.ColorSlotCount];
            for (int i = 0; i < chips.Length; i++)
            {
                int index = i;
                var x = (i - (chips.Length - 1) * 0.5f) * ChipStep;
                var ring = UiBuilder.Button(content, "Color" + i, font, sprites.Circle, "", 1, config.uiTextDark, Color.clear, Color.clear,
                    () => { Click(); SelectColor(index); });
                ring.GetComponent<Image>().type = Image.Type.Simple;
                UiBuilder.Place((RectTransform)ring.transform, top, top, new Vector2(x, ChipY), Vector2.one * (ChipSize + ChipRing));
                var chip = new Chip { ring = ring.GetComponent<Image>() };
                chip.fill = UiBuilder.Image(ring.transform, "Fill", sprites.Circle, FloorPalette.ColorOf(i), false);
                UiBuilder.Place(chip.fill.rectTransform, center, center, Vector2.zero, Vector2.one * ChipSize);
                chip.icon = UiBuilder.Image(ring.transform, "Icon", sprites.IconSprite(FloorPalette.IconOf(i)), config.uiText, false);
                UiBuilder.Place(chip.icon.rectTransform, center, center, Vector2.zero, Vector2.one * ChipSize * 0.55f);
                chip.padlock = UiBuilder.Image(ring.transform, "Lock", sprites.Lock, config.uiText, false);
                UiBuilder.Place(chip.padlock.rectTransform, center, center, Vector2.zero, Vector2.one * ChipSize * 0.5f);
                chip.level = UiBuilder.Label(ring.transform, "Level", font, "", ChipLevelFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
                UiBuilder.Place(chip.level.rectTransform, new Vector2(0.5f, 0f), center, new Vector2(0f, -ChipLevelFont * 0.7f), Vector2.zero);
                chips[i] = chip;
            }

            cards = new Card[Catalog.Skins.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                int column = i % Columns;
                int row = i / Columns;
                var button = UiBuilder.Button(content, "Skin" + i, font, sprites.Panel, "", 1, Color.white, Color.clear, Color.clear,
                    () => { Click(); SelectSkin(index); });
                var rt = UiBuilder.Place((RectTransform)button.transform, top, top,
                    new Vector2((column - (Columns - 1) * 0.5f) * CardStepX, GridTop - row * CardStepY), CardSize);
                var card = new Card { back = button.GetComponent<Image>() };
                card.icon = UiBuilder.Image(rt, "Icon", sprites.Person, config.uiNextCircle, false);
                UiBuilder.Place(card.icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 14f), Vector2.one * CardIconSize);
                card.name = UiBuilder.Label(rt, "Name", font, Catalog.Skins[i].name, CardNameFont, config.uiTextDark, TextAnchor.MiddleLeft, Color.clear);
                card.name.horizontalOverflow = HorizontalWrapMode.Wrap;
                card.name.resizeTextForBestFit = true;
                card.name.resizeTextMinSize = CardNameFont / 2;
                card.name.resizeTextMaxSize = CardNameFont;
                UiBuilder.Place(card.name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -14f), new Vector2(CardSize.x - 92f, 80f));
                card.status = UiBuilder.Label(rt, "Status", font, "", CardStatusFont, config.uiNextCircle, TextAnchor.MiddleLeft, Color.clear);
                UiBuilder.Place(card.status.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(84f + SmallCoin + 4f, 12f), new Vector2(CardSize.x - 130f, 40f));
                card.coin = UiBuilder.Image(rt, "Coin", sprites.Coin, config.uiCoin, false);
                UiBuilder.Place(card.coin.rectTransform, Vector2.zero, Vector2.zero, new Vector2(84f, 16f), Vector2.one * SmallCoin);
                cards[i] = card;
            }

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

        void Click()
        {
            sfx.Play(SfxKey.Button);
        }

        public void Open()
        {
            var save = Save();
            color = 0;
            skin = SkinIndex(save.EquippedSkin(color));
            sheet.Open("Characters");
            Refresh();
            // Close in on the preview character so it shows large above the sheet.
            var feet = building.RoomToWorld(0, new Vector3(config.roomWidth * PreviewX, 0f, 0f));
            var size = config.skinPreviewBox;
            home.FocusBox(new Bounds(feet + Vector3.up * size.y * 0.5f, size), sheet.Height + SheetCameraGap, config.skinPreviewMaxReserve);
        }

        public void Close()
        {
            sheet.Hide();
            spawner.HidePreview();
        }

        void OnSheetClosed()
        {
            spawner.HidePreview();
            Closed?.Invoke();
        }

        static int SkinIndex(string id)
        {
            for (int i = 0; i < Catalog.Skins.Length; i++)
                if (Catalog.Skins[i].id == id)
                    return i;
            return 0;
        }

        void SelectColor(int index)
        {
            var save = Save();
            if (!UnlockSchedule.IsColorSlotUnlocked(index, save))
            {
                // The next floor in order can be opened now with gems; later ones cannot yet.
                if (GemMath.IsNextFloor(index, UnlockSchedule.UnlockedFloorCount(save)))
                {
                    int before = save.coins;
                    Gems.UnlockEarly(Catalog.FloorId(index), r =>
                    {
                        Celebrate(chips[index].ring.rectTransform.position);
                        Purchased?.Invoke(r, before);
                        SelectColor(index);
                    });
                    return;
                }
                sfx.Play(SfxKey.Angry);
                fx.Punch(chips[index].ring.rectTransform, config.uiPunch);
                return;
            }
            color = index;
            skin = SkinIndex(save.EquippedSkin(color));
            Refresh();
        }

        void SelectSkin(int index)
        {
            skin = index;
            Refresh();
        }

        public void Refresh()
        {
            var save = Save();

            for (int i = 0; i < chips.Length; i++)
            {
                bool unlocked = UnlockSchedule.IsColorSlotUnlocked(i, save);
                ref var chip = ref chips[i];
                chip.ring.color = i == color ? config.uiTextDark : Color.clear;
                chip.fill.color = unlocked ? FloorPalette.ColorOf(i) : config.uiCantAfford;
                chip.icon.gameObject.SetActive(unlocked);
                chip.padlock.gameObject.SetActive(!unlocked);
                chip.level.text = unlocked ? "" : "Lv " + Catalog.FloorUnlockAt(i);
            }

            var equipped = save.EquippedSkin(color);
            for (int i = 0; i < cards.Length; i++)
            {
                var item = Catalog.Skins[i];
                ref var card = ref cards[i];
                bool owned = save.Owns(item.id);
                bool unlocked = UnlockSchedule.IsUnlocked(item.id, save);
                card.back.color = i == skin ? config.uiStarOn : Color.white;
                card.icon.color = item.id == equipped ? FloorPalette.ColorOf(color) : unlocked ? config.uiNextCircle : config.uiCantAfford;
                card.coin.gameObject.SetActive(!owned && unlocked);
                card.status.rectTransform.anchoredPosition = new Vector2(84f + (!owned && unlocked ? SmallCoin + 4f : 0f), 12f);
                card.status.text = item.id == equipped ? "Equipped" : owned ? "Owned" : unlocked ? item.price.ToString() : "Level " + item.unlockAt;
            }

            var selected = Catalog.Skins[skin];
            bool locked = !save.Owns(selected.id) && !UnlockSchedule.IsUnlocked(selected.id, save);
            ShowGemButton(locked);
            if (selected.id == equipped)
            {
                actionPrice.ShowText("Equipped");
                SetAction(config.uiEquipped, false);
            }
            else if (save.Owns(selected.id))
            {
                actionPrice.ShowText("Equip");
                SetAction(config.uiOwned, true);
            }
            else if (locked)
            {
                // The level it unlocks at, beside a gem button to have it now.
                int gems = GemMath.EarlyUnlockGems(selected.price);
                actionPrice.ShowLocked("Level " + selected.unlockAt);
                SetAction(config.uiCantAfford, false);
                gemPrice.ShowGems(gems);
                gemButtonImage.color = save.gems >= gems ? config.uiGemButton : config.uiCantAfford;
            }
            else if (save.coins >= selected.price)
            {
                actionPrice.ShowCoins(selected.price);
                SetAction(config.uiPlayButton, true);
            }
            else
            {
                int gems = GemMath.ShortfallGems(selected.price, save.coins);
                actionPrice.ShowCoinsAndGems(save.coins, gems);
                SetAction(save.gems >= gems ? config.uiGemButton : config.uiCantAfford, true);
            }

            // Live preview: the selected color wearing the selected skin, on the ground floor.
            var spot = building.RoomToWorld(0, new Vector3(config.roomWidth * PreviewX, 0f, 0f));
            spawner.ShowPreview(color, selected.id, building.transform.InverseTransformPoint(spot)).SetPatienceVisible(false);
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
            var save = Save();
            var selected = Catalog.Skins[skin];
            int before = save.coins;
            int slot = color;
            Gems.UnlockEarly(selected.id, r =>
            {
                Celebrate(((RectTransform)gemButton.transform).position);
                Purchased?.Invoke(r, before);
                // Like a purchase, the new skin goes straight on the selected color.
                Equip(selected.id, slot);
                Refresh();
            });
        }

        void Celebrate(Vector3 position)
        {
            sfx.Play(SfxKey.Buy);
            Fx.Haptic();
            fx.ConfettiBurst(position, config.purchaseConfetti, config.purchaseConfettiSpeed);
        }

        void Act()
        {
            var save = Save();
            var selected = Catalog.Skins[skin];
            if (!save.Owns(selected.id))
            {
                int before = save.coins;
                var receipt = Buy(selected.id);
                if (receipt.result == ShopResult.NotEnoughCoins)
                {
                    int slot = color;
                    Gems.BuyWithGems(selected.id, r =>
                    {
                        Celebrate(((RectTransform)action.transform).position);
                        Purchased?.Invoke(r, before);
                        Equip(selected.id, slot);
                        Refresh();
                    });
                    return;
                }
                if (!receipt.Ok)
                {
                    sfx.Play(SfxKey.Angry);
                    fx.Punch(action.transform, config.uiPunch);
                    return;
                }
                Celebrate(((RectTransform)action.transform).position);
                Purchased?.Invoke(receipt, before);
            }
            else
            {
                Click();
            }
            // Buying puts the new skin straight on the selected color.
            Equip(selected.id, color);
            Refresh();
        }
    }
}
