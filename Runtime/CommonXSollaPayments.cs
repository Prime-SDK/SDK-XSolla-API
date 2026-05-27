using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xsolla.Catalog;
using Xsolla.Core;
using Xsolla.Inventory;

namespace PrimeGames.SDK.XSolla
{
    public abstract class CommonXSollaPayments : CommonPayments
    {
        protected abstract bool IsLoggedIn();
        protected abstract void InvokeLogin(Action onSuccess, Action onError);

        private ProductData[] products = Array.Empty<ProductData>();

        public CommonXSollaPayments(IData data) : base(data) { }

        protected override ProductData GetProductDataImpl(string productTag)
        {
            return Array.Find(products, p => p.Tag == productTag);
        }

        protected override bool IsAlreadyPurchasedImpl(string productTag)
        {
            if (IsLoggedIn() == false)
            {
                return false;
            }
            return Array.Exists(Purchases, purchase => purchase == productTag);
        }

        protected override void PurchaseImpl(string productTag, Action onSuccess, Action onError = null)
        {
            InvokeLogin(
                () => {
                    XsollaCatalog.Purchase(
                        itemSku: productTag,
                        onSuccess: (orderStatus) => {
                            Logger.CreateText(this, $"OnPurchase '{orderStatus}'");
                            if (orderStatus.status == "done")
                            {
                                onSuccess?.Invoke();
                            }
                            else
                            {
                                Logger.CreateError(this, $"Purchase failed '{orderStatus}'");
                                onError?.Invoke();
                            }
                        },
                        onError: (error) => {
                            Logger.CreateError(this, $"Failed to purchase '{productTag}', '{error}'");
                            onError?.Invoke();
                        },
                        onOrderCreated: (orderData) => {
                            Logger.CreateText(this, $"OnOrderCreated '{orderData.order_id}'");
                        });
                },
                () => {
                    Logger.CreateError(this, "Unable to start purchase because player refused to login");
                    onError?.Invoke();
                }
            );
        }

        protected override void RestorePurchasesImpl(Action<IRestoreData> onRestoreData)
        {
            if (IsLoggedIn() == false)
            {
                Logger.CreateError(this, "Player is not logged in");
                Purchases = Array.Empty<string>();
                IRestoreData emptyRestoreData = new RestoreData(this, Purchases);
                onRestoreData?.Invoke(emptyRestoreData);
                return;
            }
            XsollaInventory.GetInventoryItems(
                onSuccess: (inventoryItems) =>
                {
                    if (inventoryItems?.items == null)
                    {
                        Logger.CreateError(this, "InventoryItems or items array is null");
                        Purchases = Array.Empty<string>();
                        IRestoreData emptyRestoreData = new RestoreData(this, Purchases);
                        onRestoreData?.Invoke(emptyRestoreData);
                        return;
                    }
                    Logger.CreateText(this, $"GetInventoryItems returned '{inventoryItems.items.Length}' items");
                    List<string> purchases = new();
                    foreach (InventoryItem item in inventoryItems.items)
                    {
                        if (item == null)
                        {
                            Logger.CreateError(this, "Null inventory item encountered, skipping");
                            continue;
                        }
                        string productTag = item.sku;
                        Logger.CreateText(this, $"Purchase '{productTag}'");
                        purchases.Add(productTag);
                    }
                    Purchases = purchases.ToArray();
                    IRestoreData restoreData = new RestoreData(this, Purchases);
                    onRestoreData?.Invoke(restoreData);
                },
                onError: (error) =>
                {
                    Logger.CreateError(this, $"Failed to get purchases: '{error}'");
                    Purchases = Array.Empty<string>();
                    IRestoreData emptyRestoreData = new RestoreData(this, Purchases);
                    onRestoreData?.Invoke(emptyRestoreData);
                }
                // Limit is 50 by default, be aware of that.
            );
        }

        protected void GetItems()
        {
            XsollaCatalog.GetItems(
                onSuccess: OnItemsGet,
                onError: (error) =>
                {
                    Logger.CreateError(this, $"Failed to get catalog of products: '{error}'");
                }
            );
        }

        private void OnItemsGet(StoreItems storeItems)
        {
            if (storeItems?.items == null)
            {
                Logger.CreateError(this, "StoreItems or items array is null");
                return;
            }
            Logger.CreateText(this, $"OnItemsGet '{storeItems.items.Length}' items");
            List<ProductData> products = new();
            foreach (StoreItem item in storeItems.items)
            {
                if (item == null || item.price == null)
                {
                    Logger.CreateError(this, "Null item or price encountered, skipping");
                    continue;
                }
                string productTag = item.sku;
                string priceValue = item.price.amount;
                string priceCurrency = item.price.currency;
                Logger.CreateText(this, $"Product '{productTag}', '{priceValue}', '{priceCurrency}'");
                if (!float.TryParse(priceValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float priceFloat))
                {
                    Logger.CreateError(this, $"Failed to parse price '{priceValue}' for product '{productTag}', skipping");
                    continue;
                }
                ProductData product = new(productTag, priceFloat, priceCurrency);
                products.Add(product);
            }
            this.products = products.ToArray();
            if (IsPaymentsInitialized == false)
            {
                SetInitialized();
            }
        }
    }
}