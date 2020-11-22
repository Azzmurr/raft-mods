using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;


namespace azzmurr.craftFromStorage
{
    public class CraftFromStorageMod : Mod
    {
        Harmony harmony;

        public void Start()
        {
            harmony = new Harmony("com.azzmurr.craft-from-storage");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("Craft From Storage Mod has been loaded!");
        }

        public void OnModUnload()
        {
            Debug.Log("Craft From Storage Mod has been unloaded!");
            harmony.UnpatchAll("com.azzmurr.craft-from-storage");
            Destroy(gameObject);
        }
    }


    [HarmonyPatch(typeof(StorageManager), "OpenStorage")]
    class OpenStoragePatch
    {
        static void Postfix(ref Storage_Small storage)
        {
            CraftFromStorageManager.setOpenedStorage(storage);
        }
    }

    [HarmonyPatch(typeof(StorageManager), "CloseStorage")]
    class CloseStoragePatch
    {
        static void Postfix()
        {
            CraftFromStorageManager.removeOpenedStorage();
        }
    }

    [HarmonyPatch(typeof(CostMultiple), "HasEnoughInInventory")]
    class HasEnoughInInventoryPatch
    {
        static bool Postfix(bool __result, CostMultiple __instance, Inventory inventory)
        {
            if (!CraftFromStorageManager.isUnlimitedResources()) {
                return CraftFromStorageManager.enoughInStorageInventory(__result, __instance, inventory);
            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(BuildingUI_CostBox), "SetAmountInInventory")]
    class SetAmountInInventoryPatch
    {
        static void Postfix(PlayerInventory inventory, BuildingUI_CostBox __instance)
        {
            if (!CraftFromStorageManager.isUnlimitedResources()) {
                __instance.SetAmount(__instance.GetAmount() + CraftFromStorageManager.getItemCountInInventory(__instance, null));
            }
        }
    }

    [HarmonyPatch(typeof(CraftingMenu), "CraftItem")]
    class CraftItemPatch
    {
        static void Prefix(SelectedRecipeBox ___selectedRecipeBox) {
            Item_Base itemBase = ___selectedRecipeBox.selectedRecipeItem;

            if (itemBase != null)
            {
                CostMultiple[] newCost = itemBase.settings_recipe.NewCost;
                CraftFromStorageManager.RemoveCostMultiple(newCost);
            }
        }
    }

    [HarmonyPatch(typeof(BuildingUI_Costbox_Sub_Crafting), "OnQuickCraft")]
    class OnQuickCraftPatch
    {
        static void Prefix(Item_Base ___item)
        {
            if (___item != null)
            {
                CostMultiple[] newCost = ___item.settings_recipe.NewCost;
                CraftFromStorageManager.RemoveCostMultiple(newCost);
            }
        }
    }

    class CraftFromStorageManager
    {
        static private Storage_Small storage = null;

        static public bool isUnlimitedResources()
        {
            return GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources;
        }

        static public Inventory getStorageInventory()
        {
            return CraftFromStorageManager.storage ? CraftFromStorageManager.storage.GetInventoryReference() : null;
        }

        static public bool isStorageInventoryOpened()
        {
            return CraftFromStorageManager.getStorageInventory() != null;
        }

        static private bool isInventorySameAsOpenedStorageInventory(Inventory inventory)
        {
            return CraftFromStorageManager.getStorageInventory() == inventory;
        }

        static public bool setOpenedStorage(Storage_Small storage)
        {
            if (storage != null)
            {
                CraftFromStorageManager.storage = storage;
                return true;
            }

            return false;
        }

        static public void removeOpenedStorage()
        {
            CraftFromStorageManager.storage = null;
        }

        static public bool enoughInStorageInventory(bool enouthInPlayerInventory, CostMultiple costMultiple, Inventory inventoryToCheck)
        {
            bool enough = enouthInPlayerInventory;

            if (!CraftFromStorageManager.isUnlimitedResources())
            {
                if (enough == false && CraftFromStorageManager.isStorageInventoryOpened() && !CraftFromStorageManager.isInventorySameAsOpenedStorageInventory(inventoryToCheck))
                {
                    enough = costMultiple.HasEnoughInInventory(CraftFromStorageManager.getStorageInventory());
                }
            }

            return enough;
        }

        static public int getItemCountInInventory(BuildingUI_CostBox costBox, Inventory inventory)
        {
            Inventory actualInventory = inventory != null ? inventory : CraftFromStorageManager.getStorageInventory();
            List<Item_Base> items = CraftFromStorageManager.getItemsFromCostBox(costBox);
            int itemCount = 0;

            if (actualInventory != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null)
                    {
                        itemCount += actualInventory.GetItemCount(items[i].UniqueName);
                    }
                }
            }

            return itemCount;
        }

        static public int getItemCountInInventory(CostMultiple costMultiple, Inventory inventory)
        {
            Inventory actualInventory = inventory != null ? inventory : CraftFromStorageManager.getStorageInventory();
            Item_Base[] items = costMultiple.items;
            int itemCount = 0;

            if (actualInventory != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                    {
                        itemCount += actualInventory.GetItemCount(items[i].UniqueName);
                    }
                }
            }

            return itemCount;
        }

        static public List<Item_Base> getItemsFromCostBox(BuildingUI_CostBox costBox)
        {
            return Traverse.Create(costBox).Field("items").GetValue<List<Item_Base>>();
        }

        static public void RemoveCostMultiple(CostMultiple[] costMultipleArray)
        {
            if (!CraftFromStorageManager.isUnlimitedResources())
            {
                Inventory storageInventory = CraftFromStorageManager.getStorageInventory();
                Inventory playerInventory = RAPI.GetLocalPlayer().Inventory;
                if (storageInventory != null)
                {
                    for (int i = 0; i < (int)costMultipleArray.Length; i++)
                    {
                        CostMultiple costMultiple = costMultipleArray[i];
                        int num = costMultiple.amount - CraftFromStorageManager.getItemCountInInventory(costMultiple, playerInventory);

                        for (int j = 0; j < (int)costMultiple.items.Length; j++)
                        {
                            int itemCount = storageInventory.GetItemCount(costMultiple.items[j].UniqueName);
                            if (itemCount < num)
                            {
                                storageInventory.RemoveItem(costMultiple.items[j].UniqueName, itemCount);
                                num -= itemCount;
                            }
                            else
                            {
                                storageInventory.RemoveItem(costMultiple.items[j].UniqueName, num);
                                num = 0;
                            }
                            if (num <= 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}