﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShopTileFramework
{
    class Shop
    {
        public string ShopName { get; set; }
        private static Random random = new Random();
        private Texture2D Portrait = null;
        private string Quote { get; set; }
        private int ShopPrice { get; set; }
        private ItemStock[] ItemStocks { get; set; }
        private int MaxNumItemsSoldInStore { get; set; }
        public Dictionary<ISalable, int[]> ItemPriceAndStock { get; set; }
        private string StoreCurrency { get; set; }

        public Shop(ShopPack pack, IContentPack contentPack)
        {
            ShopName = pack.ShopName;
            ShopPrice = pack.ShopPrice;
            StoreCurrency = pack.StoreCurrency;
            ItemStocks = pack.ItemStocks;
            Quote = pack.Quote;

            MaxNumItemsSoldInStore = pack.MaxNumItemsSoldInStore;

            //try and load in the portrait
            if (pack.PortraitPath != null)
            {
                try
                {
                    Portrait = contentPack.LoadAsset<Texture2D>(pack.PortraitPath);
                }
                catch (Exception ex)
                {
                    ModEntry.monitor.Log(ex.Message, LogLevel.Warn);
                }
            }
        }

        public void UpdateItemPriceAndStock()
        {
            ModEntry.monitor.Log($"Generating stock for {ShopName}", LogLevel.Trace);
            //list of all items from this ItemStock
            ItemPriceAndStock = new Dictionary<ISalable, int[]>();

            foreach (ItemStock Inventory in ItemStocks)
            {

                if (!CheckConditions(Inventory.When))
                    continue;

                int CurrencyItemID = GetIndexByName(Inventory.StockItemCurrency,Game1.objectInformation);
                int CurrencyItemStack = Inventory.StockCurrencyStack;

                Dictionary<ISalable, int[]> ItemStockInventory = new Dictionary<ISalable, int[]>();
                //StockPrice overrides ShopPrice
                int Price = Inventory.StockPrice;
                if (Price == -1)
                {
                    Price = ShopPrice;
                }

                ModEntry.monitor.Log($"Inventory price is {Price} and ShopPrice is {ShopPrice}",LogLevel.Debug);

                //add in all items specified by index
                if (Inventory.ItemIDs != null)
                {
                    foreach (var ItemID in Inventory.ItemIDs)
                    {
                        AddItem(Inventory.ItemType, ItemID, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                    }
                }

                //add in all items specified by name
                if (Inventory.ItemNames != null)
                {
                    foreach (var ItemName in Inventory.ItemNames)
                    {
                        AddItem(Inventory.ItemType, ItemName, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                    }
                }

                //add in all items from specified JA packs
                if (Inventory.JAPacks != null && ModEntry.JsonAssets != null)
                {
                    foreach (var JAPack in Inventory.JAPacks)
                    {
                        ModEntry.monitor.Log($"Adding objects from JA pack {JAPack}", LogLevel.Trace);

                        if (Inventory.ItemType == "Object")
                        {
                            var ObjectData = Game1.objectInformation;
                            var CropData = ModEntry.helper.Content.Load<Dictionary<int,
                                string>>(@"Data/Crops", ContentSource.GameContent);
                            var attemptToGetPack = ModEntry.JsonAssets.GetAllCropsFromContentPack(JAPack);
                            if (attemptToGetPack != null)
                            {
                                foreach (string ItemName in attemptToGetPack)
                                {
                                    var CropID = ModEntry.JsonAssets.GetCropId(ItemName);
                                    foreach (KeyValuePair<int, string> kvp in CropData)
                                    {
                                        //find the crop id in crop information to get seed id
                                        Int32.TryParse(kvp.Value.Split('/')[2], out int id);
                                        if (CropID == id)
                                        {
                                            AddItem(Inventory.ItemType, kvp.Key, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                                        }
                                    }
                                }
                            }
                            var FruitTreeData = ModEntry.helper.Content.Load<Dictionary<int,
                                string>>(@"Data/fruitTrees", ContentSource.GameContent);
                            attemptToGetPack = ModEntry.JsonAssets.GetAllFruitTreesFromContentPack(JAPack);
                            if (attemptToGetPack != null)
                            {
                                foreach (string ItemName in attemptToGetPack)
                                {
                                    var TreeID = ModEntry.JsonAssets.GetFruitTreeId(ItemName);

                                    foreach (KeyValuePair<int, string> kvp in FruitTreeData)
                                    {
                                        //find the tree id in fruitTrees information to get sapling id
                                        Int32.TryParse(kvp.Value.Split('/')[1], out int id);
                                        if (TreeID == id)
                                        {
                                            AddItem(Inventory.ItemType, kvp.Key, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                                        }
                                    }
                                }
                            }

                        }
                        else if (Inventory.ItemType == "BigCraftable")
                        {
                            var attemptToGetPack = ModEntry.JsonAssets.GetAllBigCraftablesFromContentPack(JAPack);
                            if (attemptToGetPack != null)
                            {
                                foreach (string CraftableName in attemptToGetPack)
                                {
                                    AddItem(Inventory.ItemType, CraftableName, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                                }
                            }
                        }
                        else if (Inventory.ItemType == "Hat")
                        {
                            var attemptToGetPack = ModEntry.JsonAssets.GetAllHatsFromContentPack(JAPack);
                            if (attemptToGetPack != null)
                            {
                                foreach (string HatName in attemptToGetPack)
                                {
                                    AddItem(Inventory.ItemType, HatName, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                                }
                            }
                        }
                        else if (Inventory.ItemType == "Weapon")
                        {
                            var attemptToGetPack = ModEntry.JsonAssets.GetAllWeaponsFromContentPack(JAPack);
                            if (attemptToGetPack != null)
                            {
                                foreach (string WeaponName in attemptToGetPack)
                                {
                                    AddItem(Inventory.ItemType, WeaponName, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                                }
                            }
                        }
                        else if (Inventory.ItemType == "Clothing")
                        {
                            var attemptToGetPack = ModEntry.JsonAssets.GetAllClothingFromContentPack(JAPack);
                            if (attemptToGetPack != null)
                            {
                                foreach (string ClothingName in ModEntry.JsonAssets.GetAllClothingFromContentPack(JAPack))
                                {
                                    AddItem(Inventory.ItemType, ClothingName, Price, Inventory.Stock, ItemStockInventory, CurrencyItemID, CurrencyItemStack);
                                }
                            }

                        }
                    }
                }
                //randomly reduce the ItemStock's inventory to the specified MaxNumItemsSoldInItemStock
                RandomizeStock(ItemStockInventory, Inventory.MaxNumItemsSoldInItemStock);
                AddToItemPriceAndStock(ItemStockInventory);

            }
            //randomly reduce store's entire inventory to the specified MaxNumItemsSoldInStore
            RandomizeStock(ItemPriceAndStock, MaxNumItemsSoldInStore);
        }

        private bool CheckConditions(string[] conditions)
        {
            //TODO write this entire thing pls
            return true;
        }

        private void AddToItemPriceAndStock(Dictionary<ISalable, int[]> dict)
        {
            foreach (var kvp in dict)
            {
                ItemPriceAndStock.Add(kvp.Key, kvp.Value);
            }
        }
        private static void RandomizeStock(Dictionary<ISalable, int[]> inventory, int MaxNum)
        {
            while (inventory.Count > MaxNum)
            {
                inventory.Remove(inventory.Keys.ElementAt(random.Next(inventory.Count)));
            }

        }

        private void AddItem(String ItemType, int itemID, int Price, int Stock, Dictionary<ISalable, int[]> itemStockInventory, int ItemCurrencyID = -1, int ItemCurrencyStack = -1)
        {
            var i = GetItem(ItemType, itemID);
            if (i != null)
            {
                int[] PriceStockCurrency;
                var price = (Price == -1)? i.salePrice() : Price;
                if (ItemCurrencyID == -1)
                {
                    PriceStockCurrency = new int[] { price, Stock };
                    
                } else if (ItemCurrencyStack == -1)
                {
                    ModEntry.monitor.Log($"I am passing in a currency item with ID of {ItemCurrencyID}", LogLevel.Warn);
                    PriceStockCurrency = new int[] { price, Stock, ItemCurrencyID };
                } else
                {
                    PriceStockCurrency = new int[] { price, Stock, ItemCurrencyID, ItemCurrencyStack };
                }

                itemStockInventory.Add(i, PriceStockCurrency);

            }
            else
            {
                ModEntry.monitor.Log($"Crop of {itemID} " +
                    $"named could not be added to {ShopName}",
                    LogLevel.Warn);
            }
        }

        private void AddItem(String ItemType, String ItemName, int Price, int Stock, Dictionary<ISalable, int[]> itemStockInventory, int ItemCurrencyID = -1, int ItemCurrencyStack = -1)
        {
            var i = GetItem(ItemType, ItemName);
            if (i != null)
            {
                int[] PriceStockCurrency;
                var price = (Price == -1) ? i.salePrice() : Price;
                if (ItemCurrencyID == -1)
                {
                    PriceStockCurrency = new int[] { price, Stock };
                }
                else if (ItemCurrencyStack == -1)
                {
                    PriceStockCurrency = new int[] { price, Stock, ItemCurrencyID };
                }
                else
                {
                    PriceStockCurrency = new int[] { price, Stock, ItemCurrencyID, ItemCurrencyStack };
                }
                itemStockInventory.Add(i, PriceStockCurrency);
            }
            else
            {
                ModEntry.monitor.Log($"{ItemType} named " +
                    $"\"{ItemName}\" could not be added to {ShopName}",
                    LogLevel.Warn);
            }
        }

        private static Item GetItem(string objectType, int index)
        {
            Item item = null;
            if (index == -1)
            {
                return null;
            }
            switch (objectType)
            {

                case "Object":
                        item = new StardewValley.Object(index, 1);
                    break;
                case "BigCraftable":
                        item = new StardewValley.Object(Vector2.Zero, index);
                    break;
                case "Clothing":
                        item = new Clothing(index);
                    break;
                case "Ring":
                        item = new Ring(index);
                    break;
                case "Hat":
                        item = new Hat(index);
                    break;
                case "Boots":
                        item = new Boots(index);
                    break;
                case "Furniture":
                        item = new Furniture(index, Vector2.Zero);
                    break;
                case "Weapon":
                        item = new MeleeWeapon(index);
                    break;
            }
            return item;
        }

        private static Item GetItem(string objectType, string name)
        {
            int index = -1;

            if (name == null)
            {
                return null;
            }

            switch (objectType)
            {

                case "Object":
                    index = GetIndexByName(name, Game1.objectInformation);
                    break;
                case "BigCraftable":
                    index = GetIndexByName(name, Game1.bigCraftablesInformation);
                    break;
                case "Clothing":
                    index = GetIndexByName(name, Game1.clothingInformation);
                    break;
                case "Ring":
                    index = GetIndexByName(name, Game1.objectInformation);
                    break;
                case "Hat":
                    index = GetIndexByName(name, ModEntry.helper.Content.Load<Dictionary<int, string>>
                        (@"Data/hats", ContentSource.GameContent));
                    break;
                case "Boots":
                        index = GetIndexByName(name, ModEntry.helper.Content.Load<Dictionary<int, string>>
                            (@"Data/Boots", ContentSource.GameContent));
                    break;
                case "Furniture":
                    index = GetIndexByName(name, ModEntry.helper.Content.Load<Dictionary<int, string>>
                            (@"Data/Furniture", ContentSource.GameContent));
                    break;
                case "Weapon":
                    index = GetIndexByName(name, ModEntry.helper.Content.Load<Dictionary<int, string>>
                            (@"Data/weapons", ContentSource.GameContent));
                    break;
            }
            return GetItem(objectType, index);
        }

        private static int GetIndexByName(string name, IDictionary<int, string> ObjectInfo)
        {
            foreach (KeyValuePair<int, string> kvp in ObjectInfo)
            {
                if (kvp.Value.StartsWith(name))
                {
                    return kvp.Key;
                }
            }
            return -1;
        }

        public void DisplayShop()
        {
            int currency = 0;
            switch(StoreCurrency){
                case "festivalScore":
                    currency = 1;
                    break;
                case "clubCoins":
                    currency = 2;
                    break;
            }

            var ShopMenu = new ShopMenu(ItemPriceAndStock,
                currency: currency);
            ShopMenu.portraitPerson = new NPC
            {
                Portrait = Portrait
            };
            ShopMenu.potraitPersonDialogue = Game1.parseText(Quote, Game1.dialogueFont, 304);
            Game1.activeClickableMenu = ShopMenu;
        }
    }
}
