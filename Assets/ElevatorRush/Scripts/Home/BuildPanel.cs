using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Build mode (home plan 5.3). On Home, invisible buttons over each unlocked floor open it.
    /// In Build mode the camera frames one room, each slot shows a "+" or swap marker, and a
    /// marker opens a bottom sheet with the slot's three options. Buying fills and equips the
    /// slot with a drop-in and confetti; owned options swap for free. Arrows move between
    /// unlocked floors and Back returns to the whole tower.
    /// </summary>
    public sealed class BuildPanel : MonoBehaviour
    {
        // Layout in reference pixels.
        const float BarBottom = 40f;
        const float ArrowSize = 140f;
        const float ArrowX = 380f;
        const float TitleWidth = 560f;
        const float TitleHeight = 140f;
        const float BackTopY = -110f;
        const float BackMargin = 50f;
        static readonly Vector2 BackSize = new Vector2(220f, 110f);
        const float CardWidth = 320f;
        const float CardHeight = 540f;
        const float CardSpacing = 345f;
        const float CardY = -20f;
        const float ThumbSize = 270f;
        const float ThumbTop = -24f;
        const float NameY = -340f;
        const float GradeY = -395f;
        const float CardButtonBottom = 22f;
        static readonly Vector2 CardButtonSize = new Vector2(280f, 110f);
        const int TitleFont = 54;
        const int SubFont = 40;
        const int NameFont = 42;
        const int GradeFont = 34;
        const int ButtonFont = 48;
        const float SheetCameraGap = 40f;

        struct Card
        {
            public RectTransform root;
            public RawImage thumb;
            public Text name;
            public Text grade;
            public Button button;
            public Image buttonImage;
            public PriceLabel price;
        }

        GameConfig config;
        Camera cam;
        Building building;
        HomeController home;
        ThumbnailRenderer thumbnails;
        DecorLayout layout;
        Fx fx;
        Sfx sfx;

        RectTransform hitLayer;
        WorldButton[] floorHits;
        RectTransform buildLayer;
        WorldButton[] markers;
        Text titleText;
        Text progressText;
        Button leftArrow;
        Button rightArrow;
        BottomSheet sheet;
        Card[] cards;
        Sprite plusSprite;
        Sprite swapSprite;

        int floor;
        int slot = -1;

        /// <summary>The save to read and the shop actions to call; set by GameManager.</summary>
        public Func<SaveData> Save { get; set; }
        public Func<string, ShopReceipt> Buy { get; set; }
        public Func<string, ShopResult> Equip { get; set; }
        /// <summary>Gem spends (a coin shortfall); set by GameManager.</summary>
        public GemFlow Gems { get; set; }

        public bool IsOpen { get; private set; }
        public int Floor => floor;

        /// <summary>Build mode opened (true) or closed (false).</summary>
        public event Action<bool> ModeChanged;
        /// <summary>A purchase went through: the receipt and the balance before it.</summary>
        public event Action<ShopReceipt, int> Purchased;

        public void Init(GameConfig config, Font font, SpriteFactory sprites, Canvas canvas, RectTransform uiRoot, RectTransform safeRoot,
            Camera camera, Building building, HomeController home, ThumbnailRenderer thumbnails, DecorLayout layout, Fx fx, Sfx sfx)
        {
            this.config = config;
            cam = camera;
            this.building = building;
            this.home = home;
            this.thumbnails = thumbnails;
            this.layout = layout;
            this.fx = fx;
            this.sfx = sfx;
            plusSprite = sprites.IconSprite(FloorIcon.Plus);
            swapSprite = sprites.Swap;
            var shadow = config.uiShadow;
            var bottom = new Vector2(0.5f, 0f);
            var center = new Vector2(0.5f, 0.5f);

            // Floor taps on the home tower.
            hitLayer = UiBuilder.Stretch(UiBuilder.Rect("FloorHits", safeRoot));
            floorHits = new WorldButton[Catalog.FloorCount];
            for (int i = 0; i < floorHits.Length; i++)
            {
                int index = i;
                floorHits[i] = WorldButton.HitArea(hitLayer, "Floor" + i, camera, canvas, () => { Click(); Enter(index); });
            }

            // Build mode: markers, Back, floor title and arrows.
            buildLayer = UiBuilder.Stretch(UiBuilder.Rect("BuildMode", safeRoot));
            markers = new WorldButton[Catalog.SlotsPerFloor];
            for (int s = 0; s < markers.Length; s++)
            {
                int index = s;
                markers[s] = WorldButton.Marker(buildLayer, "Marker" + s, camera, canvas, sprites.Circle, plusSprite,
                    config.uiMarker, config.uiMarkerIcon, config.markerSize, () => { Click(); OpenSlot(index); });
            }

            var back = UiBuilder.Button(buildLayer, "Back", font, sprites.Panel, "Back", ButtonFont, config.uiNextCircle, config.uiText, shadow, () => { Click(); Exit(); });
            UiBuilder.Place((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(BackMargin, BackTopY), BackSize);

            var titleBack = UiBuilder.Image(buildLayer, "FloorTitle", sprites.Panel, config.uiBarBack, true);
            UiBuilder.Place(titleBack.rectTransform, bottom, bottom, new Vector2(0f, BarBottom), new Vector2(TitleWidth, TitleHeight));
            titleText = UiBuilder.Label(titleBack.transform, "Title", font, "", TitleFont, config.uiText, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(titleText.rectTransform, new Vector2(0.5f, 1f), center, new Vector2(0f, -TitleHeight * 0.32f), new Vector2(TitleWidth, TitleFont));
            progressText = UiBuilder.Label(titleBack.transform, "Progress", font, "", SubFont, config.uiCoin, TextAnchor.MiddleCenter, shadow);
            UiBuilder.Place(progressText.rectTransform, bottom, center, new Vector2(0f, TitleHeight * 0.25f), new Vector2(TitleWidth, SubFont));

            leftArrow = Arrow(buildLayer, font, sprites, -1);
            rightArrow = Arrow(buildLayer, font, sprites, 1);

            // Option sheet.
            sheet = BottomSheet.Create(uiRoot, "DecorSheet", config, font, sprites, config.buildSheetHeight, Click);
            sheet.Closed += OnSheetClosed;
            cards = new Card[Catalog.OptionsPerSlot];
            for (int o = 0; o < cards.Length; o++)
                cards[o] = CreateCard(sheet.Content, font, sprites, o);

            buildLayer.gameObject.SetActive(false);
            hitLayer.gameObject.SetActive(false);
        }

        Button Arrow(RectTransform parent, Font font, SpriteFactory sprites, int direction)
        {
            var button = UiBuilder.Button(parent, direction < 0 ? "Left" : "Right", font, sprites.Circle, "", ButtonFont,
                config.uiTabButton, config.uiText, Color.clear, () => { Click(); Step(direction); });
            button.GetComponent<Image>().type = Image.Type.Simple;
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(direction * ArrowX, BarBottom + TitleHeight * 0.5f), Vector2.one * ArrowSize);
            var icon = UiBuilder.Image(button.transform, "Icon", sprites.Chevron, config.uiText, false);
            UiBuilder.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * ArrowSize * 0.55f);
            icon.rectTransform.localScale = new Vector3(direction, 1f, 1f);
            return button;
        }

        Card CreateCard(RectTransform parent, Font font, SpriteFactory sprites, int option)
        {
            var card = new Card();
            var bg = UiBuilder.Image(parent, "Option" + option, sprites.Panel, Color.white, true);
            card.root = UiBuilder.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2((option - 1) * CardSpacing, CardY), new Vector2(CardWidth, CardHeight));

            var thumbGo = UiBuilder.Rect("Thumb", card.root);
            card.thumb = thumbGo.gameObject.AddComponent<RawImage>();
            card.thumb.raycastTarget = false;
            UiBuilder.Place(thumbGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, ThumbTop), Vector2.one * ThumbSize);

            card.name = UiBuilder.Label(card.root, "Name", font, "", NameFont, config.uiTextDark, TextAnchor.MiddleCenter, Color.clear);
            card.name.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.name.resizeTextForBestFit = true;
            card.name.resizeTextMinSize = NameFont / 2;
            card.name.resizeTextMaxSize = NameFont;
            UiBuilder.Place(card.name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, NameY), new Vector2(CardWidth - 20f, NameFont * 2.2f));
            card.grade = UiBuilder.Label(card.root, "Grade", font, Catalog.OptionGrades[option], GradeFont, config.uiNextCircle, TextAnchor.MiddleCenter, Color.clear);
            UiBuilder.Place(card.grade.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, GradeY), Vector2.zero);

            int index = option;
            card.button = UiBuilder.Button(card.root, "Action", font, sprites.Panel, "", ButtonFont, config.uiPlayButton, config.uiText, config.uiShadow,
                () => Choose(index));
            card.buttonImage = card.button.GetComponent<Image>();
            UiBuilder.Place((RectTransform)card.button.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, CardButtonBottom), CardButtonSize);
            card.price = new PriceLabel((RectTransform)card.button.transform, config, font, sprites, ButtonFont, config.uiText, config.uiShadow);
            return card;
        }

        void Click()
        {
            sfx.Play(SfxKey.Button);
        }

        // ---- Home tower floor taps ----

        /// <summary>Shows the floor tap areas while the whole tower is on screen.</summary>
        public void SetHomeVisible(bool visible)
        {
            if (!visible && IsOpen)
                Exit(false);
            hitLayer.gameObject.SetActive(visible && !IsOpen);
            if (!visible)
                return;
            for (int i = 0; i < floorHits.Length; i++)
            {
                bool open = i < home.FloorCount;
                floorHits[i].gameObject.SetActive(open);
                if (open)
                    floorHits[i].Cover(building.FloorBounds(i));
            }
        }

        // ---- Build mode ----

        /// <summary>Opens Build mode on the first unlocked floor that still has an empty slot.</summary>
        public void EnterDefault()
        {
            var save = Save();
            for (int f = 0; f < home.FloorCount; f++)
                if (!TowerProgress.IsFloorComplete(save, f))
                {
                    Enter(f);
                    return;
                }
            Enter(0);
        }

        public void Enter(int target)
        {
            floor = Mathf.Clamp(target, 0, home.FloorCount - 1);
            if (!IsOpen)
            {
                IsOpen = true;
                hitLayer.gameObject.SetActive(false);
                buildLayer.gameObject.SetActive(true);
                ModeChanged?.Invoke(true);
            }
            sheet.Hide();
            slot = -1;
            home.FocusFloor(floor, config.buildCameraBottomBand);
            Refresh();
        }

        public void Exit()
        {
            Exit(true);
        }

        void Exit(bool refocus)
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            sheet.Hide();
            slot = -1;
            buildLayer.gameObject.SetActive(false);
            hitLayer.gameObject.SetActive(refocus);
            if (refocus)
                home.ShowTower();
            ModeChanged?.Invoke(false);
        }

        void Step(int direction)
        {
            int next = floor + direction;
            if (next >= 0 && next < home.FloorCount)
                Enter(next);
        }

        /// <summary>Updates the title, markers and open option cards from the save.</summary>
        public void Refresh()
        {
            var save = Save();
            titleText.text = "Floor " + floor + " · " + Catalog.RoomNames[floor];
            int filled = TowerProgress.FilledSlots(save, floor);
            progressText.text = filled == Catalog.SlotsPerFloor ? "Complete!" : filled + " / " + Catalog.SlotsPerFloor + " furnished";
            leftArrow.interactable = floor > 0;
            rightArrow.interactable = floor < home.FloorCount - 1;

            for (int s = 0; s < markers.Length; s++)
            {
                bool empty = save.EquippedDecor(floor, s) < 0;
                markers[s].Icon.sprite = empty ? plusSprite : swapSprite;
                markers[s].Follow(building.RoomToWorld(floor, layout.MarkerPoint(s)));
                markers[s].gameObject.SetActive(!sheet.IsOpen || s == slot);
            }
            if (sheet.IsOpen)
                RefreshCards(save);
        }

        // ---- Option sheet ----

        void OpenSlot(int index)
        {
            slot = index;
            sheet.Open(Catalog.SlotNames[slot] + " · " + Catalog.RoomNames[floor]);
            for (int o = 0; o < cards.Length; o++)
                cards[o].thumb.texture = thumbnails.Get(floor, slot, o);
            home.FocusFloor(floor, config.buildSheetHeight + SheetCameraGap);
            Refresh();
        }

        void OnSheetClosed()
        {
            slot = -1;
            if (IsOpen)
            {
                home.FocusFloor(floor, config.buildCameraBottomBand);
                Refresh();
            }
        }

        void RefreshCards(SaveData save)
        {
            int equipped = save.EquippedDecor(floor, slot);
            for (int o = 0; o < cards.Length; o++)
            {
                var item = Catalog.Decor(floor, slot, o);
                ref var card = ref cards[o];
                card.name.text = item.name;
                bool owned = save.Owns(item.id);
                if (o == equipped)
                {
                    card.price.ShowText("Equipped");
                    SetButton(ref card, config.uiEquipped, false);
                }
                else if (owned)
                {
                    card.price.ShowText("Equip");
                    SetButton(ref card, config.uiOwned, true);
                }
                else if (save.coins >= item.price)
                {
                    card.price.ShowCoins(item.price);
                    SetButton(ref card, config.uiPlayButton, true);
                }
                else
                {
                    // Short of coins: the coins the player has plus gems for the rest (monetization plan 2.2).
                    int gems = GemMath.ShortfallGems(item.price, save.coins);
                    card.price.ShowCoinsAndGems(save.coins, gems);
                    SetButton(ref card, save.gems >= gems ? config.uiGemButton : config.uiCantAfford, true);
                }
            }
        }

        static void SetButton(ref Card card, Color color, bool interactable)
        {
            card.buttonImage.color = color;
            card.button.interactable = interactable;
        }

        void Choose(int option)
        {
            if (slot < 0)
                return;
            var save = Save();
            var item = Catalog.Decor(floor, slot, option);
            int before = save.coins;

            if (save.Owns(item.id))
            {
                if (save.EquippedDecor(floor, slot) == option)
                    return;
                if (Equip(item.id) != ShopResult.Ok)
                    return;
                Click();
                Place(false);
            }
            else
            {
                var receipt = Buy(item.id);
                // The dialog is modal, so the same slot is still open when the player confirms.
                if (receipt.result == ShopResult.NotEnoughCoins)
                {
                    Gems.BuyWithGems(item.id, r => Bought(r, before));
                    return;
                }
                if (!receipt.Ok)
                {
                    sfx.Play(SfxKey.Angry);
                    fx.Punch(cards[option].root, config.uiPunch);
                    return;
                }
                Bought(receipt, before);
                return;
            }
            Refresh();
        }

        void Bought(ShopReceipt receipt, int coinsBefore)
        {
            sfx.Play(SfxKey.Buy);
            Fx.Haptic();
            Place(true);
            Purchased?.Invoke(receipt, coinsBefore);
            if (receipt.bonus > 0)
                CelebrateFloor(receipt.bonus, receipt.bonusGems);
            Refresh();
        }

        /// <summary>Rebuilds the slot in the room; furniture and art drop in with a bounce.</summary>
        void Place(bool bought)
        {
            var go = building.RedecorateSlot(floor, slot);
            if (go != null && slot != Catalog.SlotFlooring && slot != Catalog.SlotWall)
                DropIn.Play(go, config.dropInHeight, config.dropInTime);
            if (bought)
            {
                var screen = cam.WorldToScreenPoint(building.RoomToWorld(floor, layout.MarkerPoint(slot)));
                fx.ConfettiBurst(screen, config.purchaseConfetti, config.purchaseConfettiSpeed);
            }
        }

        void CelebrateFloor(int bonus, int gems)
        {
            sfx.Play(SfxKey.Win);
            fx.Confetti();
            Fx.Haptic();
            var center = building.FloorBounds(floor).center;
            fx.FloatText(center, "Floor complete! +" + bonus + (gems > 0 ? "  +" + gems + " gems" : ""), config.uiCoin, config.perfectTextFont);
        }
    }
}
