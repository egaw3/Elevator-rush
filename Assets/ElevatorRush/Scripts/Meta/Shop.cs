using System;

namespace ElevatorRush
{
    /// <summary>
    /// Pure buying and equipping on top of the save (home plan 5.2, 6, 7, 8). Every check happens
    /// before anything changes, so a failed call leaves the save untouched. <see cref="Changed"/>
    /// fires after every successful purchase or equip so the caller can save.
    /// Gems (monetization plan 2) can cover a coin shortfall on any coin price, and can unlock a
    /// floor, skin, tower skin or elevator tier before its level.
    /// </summary>
    public sealed class Shop
    {
        readonly SaveData save;
        readonly Wallet wallet;

        public event Action Changed;

        public Shop(SaveData save)
        {
            this.save = save;
            wallet = new Wallet(save);
        }

        public Wallet Wallet => wallet;
        int Done => save.LevelsCompleted;

        // ---- Checks (no side effects; for buttons and red dots) ----

        /// <summary>
        /// Why buying this id would fail, or Ok. Upgrade ids check their next tier. With
        /// <paramref name="coverWithGems"/>, gems pay whatever the coins do not.
        /// </summary>
        public ShopResult CheckBuy(string id, bool coverWithGems = false)
        {
            var upgrade = Catalog.FindUpgrade(id);
            if (upgrade != null)
                return CheckUpgrade(upgrade, save.UpgradeTier(id) + 1, coverWithGems);

            var item = Catalog.Find(id);
            if (item == null || !IsBuyable(item))
                return ShopResult.UnknownItem;
            if (save.Owns(id))
                return ShopResult.AlreadyOwned;
            if (!UnlockSchedule.IsUnlocked(id, save))
                return ShopResult.Locked;
            return CheckPrice(item.price, coverWithGems);
        }

        /// <summary>Why buying this 1-based tier would fail, or Ok.</summary>
        public ShopResult CheckUpgrade(UpgradeDef upgrade, int tier, bool coverWithGems = false)
        {
            int current = save.UpgradeTier(upgrade.id);
            if (current >= upgrade.MaxTier)
                return ShopResult.MaxedOut;
            if (tier <= current)
                return ShopResult.AlreadyOwned;
            if (tier != current + 1)
                return ShopResult.OutOfOrder;
            if (!UnlockSchedule.IsTierUnlocked(upgrade, tier, Done))
                return ShopResult.Locked;
            return CheckPrice(upgrade.PriceOf(tier), coverWithGems);
        }

        ShopResult CheckPrice(int price, bool coverWithGems)
        {
            if (wallet.CanAfford(price))
                return ShopResult.Ok;
            if (!coverWithGems)
                return ShopResult.NotEnoughCoins;
            return wallet.CanAffordGems(GemMath.ShortfallGems(price, save.coins)) ? ShopResult.Ok : ShopResult.NotEnoughGems;
        }

        /// <summary>Price of an item, or of the next tier of an upgrade (0 when maxed or unknown).</summary>
        public int PriceOf(string id)
        {
            var upgrade = Catalog.FindUpgrade(id);
            if (upgrade != null)
            {
                int next = save.UpgradeTier(id) + 1;
                return next <= upgrade.MaxTier ? upgrade.PriceOf(next) : 0;
            }
            var item = Catalog.Find(id);
            return item != null ? item.price : 0;
        }

        /// <summary>Gems needed on top of all the coins to buy this id; 0 when the coins suffice.</summary>
        public int ShortfallGems(string id) => GemMath.ShortfallGems(PriceOf(id), save.coins);

        // ---- Buying ----

        /// <summary>
        /// Buys an item. Decor and tower skins are equipped at once; decor that completes its floor
        /// pays the completion bonus. An upgrade id buys its next tier. With
        /// <paramref name="coverWithGems"/> and too few coins, all the coins are spent and gems pay
        /// the rest, rounded up; the rounding remainder is not refunded.
        /// </summary>
        public ShopReceipt TryBuy(string id, bool coverWithGems = false)
        {
            var upgrade = Catalog.FindUpgrade(id);
            if (upgrade != null)
                return TryBuyUpgrade(upgrade, save.UpgradeTier(id) + 1, coverWithGems);

            var check = CheckBuy(id, coverWithGems);
            if (check != ShopResult.Ok)
                return check;

            var item = Catalog.Find(id);
            Pay(item.price, out int coins, out int gems);
            save.AddOwned(id);
            int bonus = 0;
            if (item.kind == ItemKind.Decor)
            {
                save.SetEquippedDecor(item);
                bonus = TowerProgress.TryPayCompletionBonus(save, item.floor);
            }
            else if (item.kind == ItemKind.Tower)
            {
                save.equippedTower = id;
            }
            Changed?.Invoke();
            return new ShopReceipt(ShopResult.Ok, coins, bonus, gems, bonus > 0 ? Catalog.GemsPerFloorComplete : 0);
        }

        /// <summary>Buys one 1-based tier of an upgrade; tiers go in order and within the tier cap.</summary>
        public ShopReceipt TryBuyUpgrade(UpgradeDef upgrade, int tier, bool coverWithGems = false)
        {
            var check = CheckUpgrade(upgrade, tier, coverWithGems);
            if (check != ShopResult.Ok)
                return check;
            Pay(upgrade.PriceOf(tier), out int coins, out int gems);
            save.SetUpgradeTier(upgrade.id, tier);
            Changed?.Invoke();
            return new ShopReceipt(ShopResult.Ok, coins, 0, gems);
        }

        /// <summary>Pays a checked price: coins when they suffice, otherwise every coin plus the gems for the rest.</summary>
        void Pay(int price, out int coins, out int gems)
        {
            gems = GemMath.ShortfallGems(price, save.coins);
            coins = gems > 0 ? save.coins : price;
            wallet.TrySpend(coins);
            wallet.TrySpendGems(gems);
        }

        // ---- Unlocking early with gems (monetization plan 2.3) ----

        /// <summary>
        /// Gem price to unlock this id now: a floor, character skin or tower skin, or an upgrade id
        /// for its next tier. -1 when the id cannot be unlocked early.
        /// </summary>
        public int EarlyUnlockPrice(string id)
        {
            var upgrade = Catalog.FindUpgrade(id);
            if (upgrade != null)
            {
                int next = save.UpgradeTier(id) + 1;
                return next <= upgrade.MaxTier ? GemMath.EarlyUnlockGems(upgrade, next) : -1;
            }
            return GemMath.EarlyUnlockGems(Catalog.Find(id));
        }

        /// <summary>Why unlocking this id early would fail, or Ok.</summary>
        public ShopResult CheckUnlockEarly(string id)
        {
            var upgrade = Catalog.FindUpgrade(id);
            if (upgrade != null)
            {
                int next = save.UpgradeTier(id) + 1;
                if (next > upgrade.MaxTier)
                    return ShopResult.MaxedOut;
                if (UnlockSchedule.IsTierUnlocked(upgrade, next, Done))
                    return ShopResult.NotLocked;
            }
            else
            {
                var item = Catalog.Find(id);
                if (item == null || GemMath.EarlyUnlockGems(item) < 0)
                    return ShopResult.UnknownItem;
                if (item.kind == ItemKind.Floor)
                {
                    if (UnlockSchedule.IsFloorUnlocked(item.floor, save))
                        return ShopResult.NotLocked;
                    if (!GemMath.IsNextFloor(item.floor, UnlockSchedule.UnlockedFloorCount(save)))
                        return ShopResult.OutOfOrder;
                }
                else
                {
                    if (save.Owns(id))
                        return ShopResult.AlreadyOwned;
                    if (UnlockSchedule.IsUnlocked(id, save))
                        return ShopResult.NotLocked;
                }
            }
            return wallet.CanAffordGems(EarlyUnlockPrice(id)) ? ShopResult.Ok : ShopResult.NotEnoughGems;
        }

        /// <summary>
        /// Unlocks now for gems. A floor opens with its color slot (decor still costs coins); a
        /// skin or tower skin is unlocked and owned (a tower skin is also equipped, as when bought);
        /// an upgrade gets its next tier. Its later level-up shows no "Unlocked!" popup.
        /// </summary>
        public ShopReceipt TryUnlockEarly(string id)
        {
            var check = CheckUnlockEarly(id);
            if (check != ShopResult.Ok)
                return check;
            int gems = EarlyUnlockPrice(id);
            wallet.TrySpendGems(gems);

            var upgrade = Catalog.FindUpgrade(id);
            if (upgrade != null)
            {
                save.SetUpgradeTier(id, save.UpgradeTier(id) + 1);
            }
            else
            {
                var item = Catalog.Find(id);
                save.AddEarlyUnlock(id);
                if (item.kind != ItemKind.Floor)
                    save.AddOwned(id);
                if (item.kind == ItemKind.Tower)
                    save.equippedTower = id;
            }
            Changed?.Invoke();
            return new ShopReceipt(ShopResult.Ok, 0, 0, gems);
        }

        // ---- Equipping (free once owned) ----

        /// <summary>Equips an owned decor option in its slot, or an owned tower skin.</summary>
        public ShopResult TryEquip(string id)
        {
            var item = Catalog.Find(id);
            if (item == null || (item.kind != ItemKind.Decor && item.kind != ItemKind.Tower))
                return ShopResult.UnknownItem;
            if (!save.Owns(id))
                return ShopResult.NotOwned;
            if (item.kind == ItemKind.Decor)
                save.SetEquippedDecor(item);
            else
                save.equippedTower = id;
            Changed?.Invoke();
            return ShopResult.Ok;
        }

        /// <summary>Equips an owned character skin on one color slot whose floor is unlocked.</summary>
        public ShopResult TryEquipSkin(string skinId, int colorSlot)
        {
            var item = Catalog.Find(skinId);
            if (item == null || item.kind != ItemKind.Skin || colorSlot < 0 || colorSlot >= Catalog.ColorSlotCount)
                return ShopResult.UnknownItem;
            if (!save.Owns(skinId))
                return ShopResult.NotOwned;
            if (!UnlockSchedule.IsColorSlotUnlocked(colorSlot, save))
                return ShopResult.SlotLocked;
            save.SetEquippedSkin(colorSlot, skinId);
            Changed?.Invoke();
            return ShopResult.Ok;
        }

        static bool IsBuyable(CatalogItem item)
        {
            return item.kind == ItemKind.Decor || item.kind == ItemKind.Skin || item.kind == ItemKind.Tower;
        }
    }
}
