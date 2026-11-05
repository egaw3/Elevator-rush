using System;

namespace ElevatorRush
{
    /// <summary>
    /// Every gem spend from the Home panels (monetization plan 2.2 to 2.4): checks it, asks in the
    /// <see cref="ConfirmDialog"/>, and runs it through the shop, which saves. Too few gems goes to
    /// <see cref="NotEnoughGems"/> instead. Coin-only purchases never come through here. The
    /// callbacks run only after a successful spend, so the panel can celebrate and report it.
    /// </summary>
    public sealed class GemFlow
    {
        readonly ConfirmDialog dialog;
        readonly Func<SaveData> save;
        readonly Func<Shop> shop;

        /// <summary>The player is short of gems by this many (the store opens here once it exists).</summary>
        public Action<int> NotEnoughGems { get; set; }

        public GemFlow(ConfirmDialog dialog, Func<SaveData> save, Func<Shop> shop)
        {
            this.dialog = dialog;
            this.save = save;
            this.shop = shop;
        }

        /// <summary>Buys a coin-priced id the coins do not cover: all the coins plus gems for the rest.</summary>
        public void BuyWithGems(string id, Action<ShopReceipt> bought)
        {
            var data = save();
            var check = shop().CheckBuy(id, true);
            int gems = shop().ShortfallGems(id);
            if (check == ShopResult.NotEnoughGems)
            {
                NotEnoughGems?.Invoke(gems - data.gems);
                return;
            }
            if (check != ShopResult.Ok)
                return;
            dialog.Ask("Not enough coins", NameOf(id), data.coins, gems, data.gems - gems, "Buy", () =>
            {
                var receipt = shop().TryBuy(id, true);
                if (receipt.Ok)
                    bought(receipt);
            });
        }

        /// <summary>Unlocks a floor, skin, tower skin or the next elevator tier now, for gems.</summary>
        public void UnlockEarly(string id, Action<ShopReceipt> unlocked)
        {
            var data = save();
            var check = shop().CheckUnlockEarly(id);
            int gems = shop().EarlyUnlockPrice(id);
            if (check == ShopResult.NotEnoughGems)
            {
                NotEnoughGems?.Invoke(gems - data.gems);
                return;
            }
            if (check != ShopResult.Ok)
                return;
            dialog.Ask("Unlock now?", NameOf(id), 0, gems, data.gems - gems, "Unlock", () =>
            {
                var receipt = shop().TryUnlockEarly(id);
                if (receipt.Ok)
                    unlocked(receipt);
            });
        }

        /// <summary>A display name; an upgrade id names its next tier.</summary>
        string NameOf(string id)
        {
            var upgrade = Catalog.FindUpgrade(id);
            if (upgrade != null)
                return upgrade.name + " tier " + (save().UpgradeTier(id) + 1);
            var item = Catalog.Find(id);
            if (item != null && item.kind == ItemKind.Floor)
                return "Floor " + item.floor + " · " + item.name;
            return Catalog.NameOf(id);
        }
    }
}
